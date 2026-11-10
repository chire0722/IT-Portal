using System.IO;
using IT_Form.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;

namespace IT_Form.Tests.Helpers;

public sealed class SensitiveFileStorageAtomicSaveTests
{
    [Theory]
    [InlineData("/secure-files/resignations/..")]
    [InlineData("/secure-files/resignations/.")]
    [InlineData("/secure-files/resignations/sub/../document.pdf")]
    public void TryExtractCategoryAndFileName_RejectsDotSegments(string storedPath)
    {
        Assert.False(SensitiveFileStorage.TryExtractCategoryAndFileName(
            storedPath,
            out _,
            out _));
    }

    [Fact]
    public void BuildStoredPath_RejectsDotFileName()
    {
        Assert.Throws<ArgumentException>(() =>
            SensitiveFileStorage.BuildStoredPath(
                SensitiveFileStorage.ResignationsCategory,
                ".."));
    }

    [Fact]
    public async Task SaveAsync_RemovesPartialTemporaryFile_WhenCopyFails()
    {
        var root = Path.Combine(Path.GetTempPath(), $"it-portal-upload-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(x => x.ContentRootPath).Returns(root);

            var file = new Mock<IFormFile>();
            file.Setup(x => x.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns<Stream, CancellationToken>(async (destination, cancellationToken) =>
                {
                    await destination.WriteAsync(new byte[] { 0x25, 0x50, 0x44, 0x46 }, cancellationToken);
                    throw new IOException("Simulated interrupted upload.");
                });

            await Assert.ThrowsAsync<IOException>(() =>
                SensitiveFileStorage.SaveAsync(
                    file.Object,
                    environment.Object,
                    SensitiveFileStorage.OffboardingCategory,
                    "employee-document.pdf"));

            var targetDirectory = Path.Combine(
                root,
                "App_Data",
                SensitiveFileStorage.SecureFolderName,
                SensitiveFileStorage.OffboardingCategory);

            Assert.True(Directory.Exists(targetDirectory));
            Assert.Empty(Directory.GetFiles(targetDirectory));
            Assert.False(File.Exists(Path.Combine(targetDirectory, "employee-document.pdf")));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SaveAsync_PublishesCompletedFile_AndReturnsSecureRoute()
    {
        var root = Path.Combine(Path.GetTempPath(), $"it-portal-upload-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(x => x.ContentRootPath).Returns(root);

            var file = new Mock<IFormFile>();
            file.Setup(x => x.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns<Stream, CancellationToken>(async (destination, cancellationToken) =>
                {
                    await destination.WriteAsync(new byte[] { 1, 2, 3, 4 }, cancellationToken);
                });

            var storedPath = await SensitiveFileStorage.SaveAsync(
                file.Object,
                environment.Object,
                SensitiveFileStorage.OffboardingCategory,
                "employee-document.pdf");

            var physicalPath = Path.Combine(
                root,
                "App_Data",
                SensitiveFileStorage.SecureFolderName,
                SensitiveFileStorage.OffboardingCategory,
                "employee-document.pdf");

            Assert.Equal("/secure-files/offboarding/employee-document.pdf", storedPath);
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, await File.ReadAllBytesAsync(physicalPath));
            Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(physicalPath)!,
                ".upload-*.tmp"));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SaveAsync_DoesNotOverwriteExistingFile_WhenFinalNameCollides()
    {
        var root = Path.Combine(Path.GetTempPath(), $"it-portal-upload-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(x => x.ContentRootPath).Returns(root);

            var targetDirectory = Path.Combine(
                root,
                "App_Data",
                SensitiveFileStorage.SecureFolderName,
                SensitiveFileStorage.OffboardingCategory);
            Directory.CreateDirectory(targetDirectory);
            var physicalPath = Path.Combine(targetDirectory, "employee-document.pdf");
            await File.WriteAllBytesAsync(physicalPath, new byte[] { 9, 9, 9 });

            var file = new Mock<IFormFile>();
            file.Setup(x => x.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns<Stream, CancellationToken>(async (destination, cancellationToken) =>
                {
                    await destination.WriteAsync(new byte[] { 1, 2, 3 }, cancellationToken);
                });

            await Assert.ThrowsAsync<IOException>(() =>
                SensitiveFileStorage.SaveAsync(
                    file.Object,
                    environment.Object,
                    SensitiveFileStorage.OffboardingCategory,
                    "employee-document.pdf"));

            Assert.Equal(new byte[] { 9, 9, 9 }, await File.ReadAllBytesAsync(physicalPath));
            Assert.Empty(Directory.GetFiles(targetDirectory, ".upload-*.tmp"));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
