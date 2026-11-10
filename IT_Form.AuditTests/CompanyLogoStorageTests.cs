using IT_Form.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;

namespace IT_Form.AuditTests;

public sealed class CompanyLogoStorageTests
{
    [Fact]
    public async Task SaveAsync_WritesUniqueLogoInsideManagedDirectory()
    {
        var webRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(webRoot);
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(x => x.WebRootPath).Returns(webRoot);

        try
        {
            var bytes = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
            await using var source = new MemoryStream(bytes);
            IFormFile file = new FormFile(source, 0, source.Length, "logo", "logo.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };

            var relativePath = await CompanyLogoStorage.SaveAsync(environment.Object, file);

            Assert.NotNull(relativePath);
            Assert.StartsWith("uploads/companies/logos/", relativePath);
            var storedPath = Path.GetFullPath(Path.Combine(webRoot, relativePath));
            Assert.True(File.Exists(storedPath));
            Assert.Equal(storedPath, CompanyLogoStorage.ResolveManagedPath(environment.Object, relativePath));
            Assert.Empty(Directory.GetFiles(Path.Combine(webRoot, "uploads", "companies", "logos"), "*.uploading"));
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }

    [Fact]
    public void DeleteIfManaged_DoesNotDeleteOutsideManagedLogoDirectory()
    {
        var webRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(webRoot);
        var outsideFile = Path.Combine(webRoot, "keep.txt");
        File.WriteAllText(outsideFile, "keep");
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(x => x.WebRootPath).Returns(webRoot);

        try
        {
            const string traversalPath = "uploads/companies/logos/../../../keep.txt";
            Assert.Null(CompanyLogoStorage.ResolveManagedPath(environment.Object, traversalPath));
            CompanyLogoStorage.DeleteIfManaged(environment.Object, traversalPath);

            Assert.True(File.Exists(outsideFile));
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }
}

