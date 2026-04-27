# IT Portal System — User Guide

**Version:** 1.0 &nbsp;&nbsp;|&nbsp;&nbsp; **Last Updated:** April 2026 &nbsp;&nbsp;|&nbsp;&nbsp; **Audience:** All Staff

---

<br>

## Table of Contents

1. [Introduction](#1-introduction)
2. [Getting Started](#2-getting-started)
   - 2.1 [Logging In](#21-logging-in)
   - 2.2 [Forgot Password & OTP Verification](#22-forgot-password--otp-verification)
   - 2.3 [Your Profile](#23-your-profile)
3. [Dashboard Overview](#3-dashboard-overview)
   - 3.1 [IT Admin Dashboard](#31-it-admin-dashboard)
   - 3.2 [Analytics Page](#32-analytics-page)
4. [Onboarding — New Employee Setup](#4-onboarding--new-employee-setup)
   - 4.1 [Creating an Onboarding Request (HR)](#41-creating-an-onboarding-request-hr)
   - 4.2 [IT Setup Wizard (IT Staff)](#42-it-setup-wizard-it-staff)
   - 4.3 [Employee Acknowledgement](#43-employee-acknowledgement)
   - 4.4 [Tracking Onboarding Progress](#44-tracking-onboarding-progress)
5. [Offboarding — Employee Exit Process](#5-offboarding--employee-exit-process)
   - 5.1 [Creating an Offboarding Request (HR)](#51-creating-an-offboarding-request-hr)
   - 5.2 [Approval & Signature Flow](#52-approval--signature-flow)
   - 5.3 [IT Deprovisioning (IT Staff)](#53-it-deprovisioning-it-staff)
   - 5.4 [Employee Acknowledgement](#54-employee-acknowledgement)
6. [IT Asset Management](#6-it-asset-management)
   - 6.1 [Viewing the Asset Inventory](#61-viewing-the-asset-inventory)
   - 6.2 [Adding a New Asset](#62-adding-a-new-asset)
   - 6.3 [Editing & Assigning Assets](#63-editing--assigning-assets)
   - 6.4 [Bulk Import](#64-bulk-import)
   - 6.5 [Device Verification](#65-device-verification)
7. [Software & License Management](#7-software--license-management)
   - 7.1 [Software Catalog](#71-software-catalog)
   - 7.2 [Managing License Versions & Keys](#72-managing-license-versions--keys)
8. [Email Notifications](#8-email-notifications)
9. [Digital Signatures & PDF Export](#9-digital-signatures--pdf-export)
10. [Understanding Form Statuses](#10-understanding-form-statuses)
11. [User Roles & Permissions](#11-user-roles--permissions)
12. [Configuration (IT Admin Only)](#12-configuration-it-admin-only)
13. [Frequently Asked Questions (FAQ)](#13-frequently-asked-questions-faq)

---

<br>

## 1. Introduction

Welcome to the **IT Portal System** — your company's centralised platform for managing employee on-boarding, off-boarding, IT assets, and software licenses.

**What this system does for you:**

| Capability | Description |
|---|---|
| **Employee Onboarding** | Streamlines the process of setting up a new joiner with hardware, software, email groups, and shared folders. |
| **Employee Offboarding** | Manages the secure exit of a departing employee — revoking access, collecting hardware, and archiving data. |
| **IT Asset Tracking** | Keeps a real-time inventory of every laptop, desktop, monitor, and peripheral in the organisation. |
| **Software Licensing** | Tracks software license contracts, keys, seat utilisation, and expiry dates across the company. |
| **Digital Signatures** | Provides tamper-evident electronic signatures so forms can be approved without paper. |
| **SLA Monitoring** | Automatically flags requests that exceed the target turnaround time. |
| **Audit Trail** | Logs every action for compliance and accountability. |

> [!NOTE]
> This guide is written for everyday users. You do not need any technical background to follow the steps described here.

<br>

---

<br>

## 2. Getting Started

### 2.1 Logging In

Open your web browser and navigate to the IT Portal address provided by your IT department.

![Login Page](C:\Users\it.dept\.gemini\antigravity\brain\0dc5ac1b-4da9-415a-a6d8-0259ae618eeb\login_page.png)

**Steps:**

1. Enter your **Username** in the first field.
2. Enter your **Password** in the second field. You can click the 👁 (eye) icon to reveal the password while typing.
3. *(Optional)* Tick **Remember me** if you want the system to keep you logged in on this device.
4. Click the **Login** button.

> [!TIP]
> If this is your first time logging in—or if IT has recently reset your password—you will be asked to change your password immediately. Follow the on-screen instructions to set a new, strong password.

<br>

### 2.2 Forgot Password & OTP Verification

If you have forgotten your password:

1. Click the **Forgot Password?** link on the login page.
2. Enter your **Username** and click **Submit**.
3. A one-time passcode (OTP) will be sent to your registered email address.
4. Enter the OTP on the **Verify OTP** page within the time limit.
5. If the code is correct, you will be redirected to a **Reset Password** page.
6. Enter your new password, confirm it, and click **Reset Password**.

> [!IMPORTANT]
> If the OTP expires, click **Resend OTP** to receive a fresh code. After multiple failed attempts, your account may be temporarily locked for security purposes.

<br>

### 2.3 Your Profile

Once logged in, click your **name** at the bottom-left corner of the sidebar to access your **Profile** page.

On your Profile page, you can:

- **Update your display name** and **job title** — this information appears in email signatures sent from the portal.
- **Upload your email signature image** — this image (e.g., your company logo block or QR code) is attached to notification emails you send through the system.
- **Draw your digital signature** — this reusable signature is automatically applied when you sign off on onboarding or offboarding requests.

<br>

---

<br>

## 3. Dashboard Overview

After logging in, you are taken to the **Dashboard** — your personalised starting page.

![Dashboard](C:\Users\it.dept\.gemini\antigravity\brain\0dc5ac1b-4da9-415a-a6d8-0259ae618eeb\dashboard.png)

### 3.1 IT Admin Dashboard

The IT Admin Dashboard is divided into several sections:

| Section | What It Shows |
|---|---|
| **Needs Your Attention** | Requests awaiting employee acknowledgement signatures, listed by employee name and type (Onboarding / Offboarding). |
| **Core Metrics** | Monthly count of onboarding and offboarding requests, total assets, and expiry alerts. |
| **Assets & Software** | Distribution of assets by type and status, plus software license utilisation charts. |
| **System Health** | Recent failed commands, total audit log entries, and active user count. |
| **Recent Failed Commands** | A quick alert panel showing any system-level errors caught by the audit pipeline. |

You can switch the reporting year using the **Year** dropdown at the top-right corner of the page.

<br>

### 3.2 Analytics Page

For HR and IT staff, click **Analytics** in the sidebar to view advanced charts and KPIs:

- **Monthly Trend Charts** — Line graphs showing onboarding vs. offboarding trends over the year.
- **Department & Company Breakdowns** — Bar charts showing which teams are hiring or losing staff.
- **Status Distribution** — Donut charts showing how many requests are in each status.
- **SLA Compliance** — Bottleneck analysis showing average processing time at each phase.
- **Year-over-Year Comparisons** — Percentage changes compared to the previous year.
- **Average Tenure** — Average employment duration (in days) for employees who departed this year.

<br>

---

<br>

## 4. Onboarding — New Employee Setup

The onboarding process is a **multi-step wizard** that moves through the following phases:

```
HR Creates Request → HR Signs Off → IT Provisions Assets & Software → IT Signs Off → Employee Acknowledges
```

<br>

### 4.1 Creating an Onboarding Request (HR)

![Onboarding Dashboard](C:\Users\it.dept\.gemini\antigravity\brain\0dc5ac1b-4da9-415a-a6d8-0259ae618eeb\onboarding.png)

> [!NOTE]
> Only users with the **HR** role can create new onboarding requests.

**Step 1 — Employee Details:**

1. From the sidebar, click **Onboarding**.
2. Click the **+ New Onboarding** button (or navigate to **Create**).
3. Fill in the new employee's information:
   - **Full Name** (required, max 100 characters)
   - **Employee ID**
   - **Designation** (job title)
   - **Department** — select from the dropdown, or choose *Others* and type the department name
   - **Company / Branch** — select from the dropdown
   - **Desk Number**
   - **Join Date**
   - **Email Address**
   - **Contact Number**
   - **HOD Email** — the Head of Department's email address
4. Agree to the **PDPA Consent** checkbox.
5. Click **Next** to proceed.

**Step 2 — Software, Email Groups & Shared Folders:**

1. **Software Checklist** — Tick the software titles the new employee will need (e.g., Microsoft Office, Adobe Acrobat, Google Chrome, FortiClient VPN, etc.). If you need software not listed, tick *Other Software* and type the name.
2. **Email Groups** — Select the email distribution groups the employee should join (e.g., Precast Group).
3. **Shared Mailboxes** — Select shared mailboxes the employee will need access to (e.g., Account, Logistic, Admin, etc.). If needed, tick *Other Shared Mailbox* and type the details.
4. **Server Folder Permissions** — Select the shared server folders the employee needs. If needed, tick *Other Folder* and enter details.
5. Click **Save as Draft** to save progress, or click **Submit** to proceed. You may also apply your **Digital Signature** using the signature pad, then click **Sign & Submit**.

> [!TIP]
> You can save a draft at any time and return to it later. Draft requests appear with a "Draft" status badge on the Onboarding Dashboard.

<br>

### 4.2 IT Setup Wizard (IT Staff)

Once HR submits and signs the onboarding form, the request moves to **Pending IT Approval** status. IT staff can now begin provisioning.

> [!NOTE]
> Only users with the **IT** role can perform IT setup steps.

**To begin IT setup:**

1. From the **Onboarding Dashboard**, find the request with status **Pending IT Approval**.
2. Click the 👁 **View** icon, then click the **Start IT Setup** button.
3. You are guided through a **5-step wizard**:

**Step 3 — Asset Assignment:**
- Review the list of assets requested by HR.
- Assign physical IT assets from the inventory (e.g., laptop, monitor, keyboard).
- For each asset, select the **Asset Type**, enter the **Asset Tag**, **Serial Number**, **Brand/Model**, and quantity.
- Add remarks if needed.
- Click **Save & Continue** to proceed, or **Save Draft** to save progress.

**Step 4 — Software Installation & Folder Setup:**
- Review the software checklist and mark each item as installed.
- Review Server Folder permissions and grant access as required.
- Add remarks for each section.
- Click **Save & Continue**.

**Step 5 — IT Checklist & Sign-Off:**
- Complete the **IT Setup Checklist** by ticking each item:

| Checklist Item | Description |
|---|---|
| Physical Cleaning | Device has been physically cleaned and inspected |
| O365 Setup | Microsoft 365 account created and configured |
| Windows Login | Windows domain account created |
| Email Setup | Email client configured and tested |
| Specific Software | Specialised software installed per request |
| General Software | Standard software bundle installed |
| ABC Group | Added to company email group |
| Email Mail Group | Added to departmental mail group |
| Domain Join | Device joined to the company domain |
| Shared Drive | Network shared drives mapped |
| Wi-Fi Setup | Wireless network configured |
| Printer Setup | Network printers added |
| Scan Folder | Scan-to-folder configured |
| VPN Setup | VPN client installed and configured |

- Enter any **remarks** about the setup.
- Draw your **IT Admin Signature** on the signature pad.
- Click **Complete Setup** to finalise.

The status changes to **Pending Employee Sign**, and the system will prompt you to send an acknowledgement email to the new employee.

<br>

### 4.3 Employee Acknowledgement

After IT completes the setup:

1. The employee receives an email with a unique acknowledgement link.
2. Clicking the link opens a page showing a summary of everything that was set up.
3. The employee can **download the PDF** of the full onboarding form for their records.
4. They draw their signature on the digital signature pad and click **Submit Signature**.
5. The request status changes to **Completed**.

> [!IMPORTANT]
> The acknowledgement link is unique and secure. If the link needs to be regenerated (e.g., the employee lost the email), an HR user can click **Regenerate Token** on the request details page to create a new link.

<br>

### 4.4 Tracking Onboarding Progress

From the **Onboarding Dashboard**, you can:

- **Search** by employee name, ID, or department using the search bar.
- **Filter by status** using the **Status Filter** dropdown.
- **Sort columns** by clicking the column headers (Employee, Department, Status, Due Date).
- **View details** by clicking the 👁 icon in the Actions column.
- **Download PDF** by clicking the 📄 icon in the Actions column.
- **Export to CSV** by clicking the **Export CSV** button (HR only).
- **Bulk export PDFs** by selecting multiple rows with checkboxes and clicking **Export Bulk PDF** (HR only).

Requests that exceed the SLA target days display an **⏰ Overdue** badge in red next to the due date.

<br>

---

<br>

## 5. Offboarding — Employee Exit Process

The offboarding process handles the departure of an employee, ensuring all access is revoked and hardware is returned.

```
HR Creates Request → HR Signs → HOD Signs → IT Processes → IT Signs → Employee Acknowledges
```

<br>

### 5.1 Creating an Offboarding Request (HR)

![Offboarding Dashboard](C:\Users\it.dept\.gemini\antigravity\brain\0dc5ac1b-4da9-415a-a6d8-0259ae618eeb\offboarding.png)

> [!NOTE]
> Only users with the **HR** role can create new offboarding requests.

**Steps:**

1. From the sidebar, click **Offboarding**.
2. Click the **+ New Offboarding** button.
3. Fill in the departing employee's details:
   - **Full Name** (required)
   - **Employee ID**
   - **Department** (required)
   - **Company / Branch**
   - **Last Day of Work** (required)
   - **Email Address**
   - **HOD Email** (required) — the Head of Department who will co-sign
   - **Assigned HOD** — select from the dropdown if available
4. *(Optional)* Upload a **Resignation Letter** (PDF, image, or document file).
5. Agree to the **PDPA Consent** checkbox.
6. Click **Create** to submit the request.

> [!TIP]
> If the departing employee was previously onboarded through this system, you can link the offboarding request to their original onboarding record. This automatically pre-fills employee details and helps IT know exactly what was originally provisioned.

<br>

### 5.2 Approval & Signature Flow

The offboarding request requires **three signatures** before reaching the employee:

| Step | Who Signs | What Happens |
|---|---|---|
| 1. HR Sign-Off | HR Staff | HR initiates and signs the form. Status moves to **Pending Approval**. |
| 2. Checker Approval | A second HR user | A different HR user verifies and approves. Status moves to **Pending HOD Approval**. |
| 3. HOD Sign-Off | Head of Department | The HOD reviews and signs. Status moves to **Pending IT Approval**. |
| 4. IT Sign-Off | IT Staff | IT completes deprovisioning and signs. Status moves to **Pending Employee Sign**. |

At each step, the system prompts a notification email to be sent to the next approver.

<br>

### 5.3 IT Deprovisioning (IT Staff)

Once the HOD has signed, IT can proceed:

1. From the **Offboarding Dashboard**, find the request with status **Pending IT Approval**.
2. Click the 👁 **View** icon, then click **Start IT Setup**.

**Step 1 — Hardware Return & IT Deactivation:**
- Record which IT assets have been returned:
  - **Laptop Returned** ✅
  - **Accessories Returned** ✅
- Note if any hardware is **damaged** and upload a photo if needed.
- Upload a **hardware return photo** as evidence.
- Record any **missing item fines** and add remarks.
- Complete the **IT Deactivation Checklist**:

| Checklist Item | Description |
|---|---|
| Physical Cleaning | Returned device cleaned and wiped |
| O365 Deactivate | Microsoft 365 license removed |
| Windows Disable | Active Directory account disabled |
| Email Sign-Out | Email sessions terminated on all devices |
| Specific Software Deactivate | Licensed software uninstalled or deactivated |
| General Software Deactivate | Standard software removed |
| ABC Group Remove | Removed from company email group |
| Email Mail Group Remove | Removed from departmental mail group |
| Domain Disjoin | Device removed from domain |
| Shared Drive Remove | Network share access revoked |
| Wi-Fi Disable | Wireless access removed |
| Printer Remove | Printer access revoked |
| Scan Folder Remove | Scan folder access revoked |
| VPN Disable | VPN credentials revoked |

- Click **Save & Continue**.

**Step 2 — Follow-Up & IT Sign-Off:**
- Confirm post-exit actions:
  - **Data Backup Done** ✅
  - **Email Backup Done** ✅
  - **Email Forwarding Done** ✅ — enter the forwarding recipient's name
  - **License Removal Done** ✅
- Add any **follow-up remarks**.
- Draw your **IT Admin Signature** and click **Complete**.

<br>

### 5.4 Employee Acknowledgement

The same digital acknowledgement process as onboarding applies:

1. The employee receives an email with a unique link.
2. They review the offboarding summary and download the PDF.
3. They sign digitally and submit.
4. The request moves to **Completed** status.

<br>

---

<br>

## 6. IT Asset Management

The IT Asset module lets IT staff track every piece of hardware in the organisation.

![IT Asset Inventory](C:\Users\it.dept\.gemini\antigravity\brain\0dc5ac1b-4da9-415a-a6d8-0259ae618eeb\it_assets.png)

> [!NOTE]
> Only users with the **IT** role can access the IT Asset module.

<br>

### 6.1 Viewing the Asset Inventory

The **IT Asset Inventory** page displays:

- **Summary Cards** — Total Assets, Issued Assets, and Expired Warranty count.
- **Search & Filter** — Search by Asset Tag, Serial Number, or Model. Filter by **Asset Type**, **Status**, or **Company**.
- **Asset Table** — A sortable list showing Asset Tag, Type, Model, Serial Number, Warranty Detail, Assigned To, and Status.

**Asset Types available:**

| Type | Description |
|---|---|
| Laptop | Portable computers |
| Desktop PC | Stationary workstations |
| Monitor | Display screens |
| USB | USB hubs or devices |
| USB LAN Adaptor | Ethernet-to-USB adapters |
| Keyboard | External keyboards |
| Headset | Audio headsets |
| Docking Station | Laptop docking stations |
| Network Cable | Ethernet cables |
| USB Drive | Portable storage drives |
| Webcam | External cameras |
| Others | Any other IT equipment |

<br>

### 6.2 Adding a New Asset

1. Click the **+ Add New Asset** button in the top-right corner.
2. Fill in the required fields:
   - **Asset Tag** (required, unique identifier — e.g., "YTCP-LAP001")
   - **Asset Type** (required — select from dropdown)
   - **Serial Number**
   - **Model / Make**
   - **Company** — assigns ownership to a specific company
   - **Location**
   - **Purchase Date**
   - **Warranty Expiry Date**
   - **RAM** (e.g., "16GB")
   - **DDR Type** (DDR3, DDR4, DDR5, etc.)
   - **Storage** (e.g., "512GB SSD")
   - **Windows Version** (select from registered OS licenses)
3. *(Optional)* If the asset status is **Issued**, select the **Assigned Employee** from the dropdown or type a custom name.
4. Click **Create** to save.

<br>

### 6.3 Editing & Assigning Assets

- Click the ✏️ **Edit** icon on any asset row.
- Update any field, change the status, or reassign the asset to a different employee.
- Click **Save** to apply changes.

**Asset Statuses:**

| Status | Meaning |
|---|---|
| **Available** | The asset is in stock and ready to be assigned. |
| **Issued** | The asset is currently assigned to an employee. |
| **Under Repair** | The asset is being serviced or repaired. |
| **Disposed** | The asset has been retired and disposed of. |
| **Lost** | The asset has been reported lost or stolen. |

**Warranty Management:**
- Assets with an upcoming or expired warranty display an alert badge.
- Click the **No Extend Warranty** toggle to suppress expiry notifications for assets you do not intend to renew.

<br>

### 6.4 Bulk Import

To import multiple assets at once:

1. Click **Bulk Import** at the top of the page.
2. Click **Download Template** to get the Excel import template.
3. Fill in the spreadsheet (Asset Tag, Type, Status, Model, dates, etc.).  
   *The template includes dropdown validation for Asset Type, Status, DDR Type, and Windows Version.*
4. Click **Choose File**, select your completed spreadsheet, and click **Import**.
5. A success message will confirm how many assets were imported. Any errors (e.g., duplicate Asset Tags) are reported.

> [!WARNING]
> Each **Asset Tag** must be unique. If a tag already exists in the system, that row will be skipped during import.

<br>

### 6.5 Device Verification

IT staff can remotely verify and audit a device's hardware fingerprint:

1. Click the **Generate Verification Link** button on the IT Asset Inventory page.
2. Select an employee from the dropdown (or leave blank for asset-only verification).
3. The system generates a **unique verification URL** and a downloadable ZIP file.
4. Share the link or ZIP with the employee. They run the provided script, which automatically:
   - Captures the device's hardware fingerprint (CPU, RAM, storage, serial numbers).
   - Sends the data back to the IT Portal.
   - Updates the asset record with the latest hardware audit.
5. If the hardware fingerprint **does not match** the previously recorded one, a mismatch warning is flagged on the asset.

<br>

---

<br>

## 7. Software & License Management

The Software module manages your organisation's software catalogue, licensing contracts, and individual keys.

![Software Inventory](C:\Users\it.dept\.gemini\antigravity\brain\0dc5ac1b-4da9-415a-a6d8-0259ae618eeb\software.png)

> [!NOTE]
> Only users with the **IT** role can manage software and licenses.

<br>

### 7.1 Software Catalog

The **Software Inventory** page shows:

- **Summary Cards** — Total Software titles, License Cap (total seats), Utilisation percentage, Expiring Soon count, and Expired Licenses count.
- **Software List** — Each software title displays its name, badges (Mandatory, Free, Active), and the number of licence versions.

**Adding a New Software:**

1. Click the **+ Add Software** button.
2. Enter the **Software Name** (e.g., "Adobe Acrobat").
3. Set options:
   - **Is Mandatory** — automatically selected for all new employee onboarding forms.
   - **Is Free** — no licence key required.
   - **Is Active** — visible in the system.
   - **Has Multiple Versions** — allows adding multiple licence editions (e.g., "Adobe Acrobat DC", "Adobe Acrobat 2020").
4. Click **Create**.

<br>

### 7.2 Managing License Versions & Keys

Click the **▼ expand** arrow next to a software title to see its licence versions.

**Adding a Licence Version:**

1. Click **Add Version** (or the ⊕ icon).
2. Enter:
   - **Version Name** (e.g., "Microsoft Office 2024 LTSC")
   - **Description**
   - **Vendor Name** and **Support Contact**
   - **Is Subscription** — tick if this is a recurring subscription (requires an expiry date).
   - **Is Free** — tick if no payment or key is required.
   - **Expiry Date** — the contract expiry date.
   - **Contract Limit** — the maximum number of seats/licences.
   - **Company** — assign this licence to a specific company.
3. Click **Save**.

**Managing License Keys:**

- Click **Manage Keys** to view, add, edit, or delete individual licence keys.
- Each key can be **assigned** to an employee from the onboarding records.
- For subscription-based licenses, use **Quick Allocate Seat** to increment the used count without entering a key.

> [!TIP]
> The utilisation percentage on the dashboard helps you monitor whether you are approaching your contract limit. License keys are encrypted for security and are only visible to IT staff.

<br>

---

<br>

## 8. Email Notifications

The IT Portal integrates with your company email system to send professional notification emails at key points in the workflow.

**When emails are triggered:**

| Event | Recipient | Purpose |
|---|---|---|
| HR submits onboarding request | IT Department | Notify IT to begin provisioning |
| IT completes onboarding | Employee | Send acknowledgement link |
| HR submits offboarding request | HOD | Request manager sign-off |
| HOD signs offboarding | IT Department | Notify IT to begin deprovisioning |
| IT completes offboarding | Employee | Send acknowledgement link |
| Request is rejected | Original submitter | Inform of rejection with reason |
| HOD approval reminder (nudge) | HOD | Follow-up for pending signatures |

**How it works:**

1. After signing a form, the system automatically opens an **Email Confirmation** page.
2. You can review the pre-filled email content:
   - **To** and **CC** recipients (editable)
   - **Subject** line
   - **Email body** with professional HTML formatting
3. If an attachment is included (e.g., the acknowledgement PDF), you can click **Preview Attachment** to view it.
4. Click **Send Email** to dispatch the notification.

> [!NOTE]
> Emails are sent through your company's email server (e.g., Microsoft Outlook). The email includes your personal signature image if you have uploaded one in your profile.

<br>

---

<br>

## 9. Digital Signatures & PDF Export

### Digital Signatures

The IT Portal uses **tamper-evident digital signatures** to ensure the integrity of every approval:

- Each signature is drawn on a digital pad embedded in the form.
- The signature image, signer name, and exact timestamp are recorded.
- An integrity check (HMAC) is applied to prevent any tampering after signing.

**Who signs what:**

| Form | Signers |
|---|---|
| **Onboarding** | HR (Requester) → IT Admin → Employee |
| **Offboarding** | HR → HOD (Manager) → IT Admin → Employee |

### PDF Export

Every onboarding and offboarding request can be exported as a professional PDF:

- Click the 📄 icon on any request row to download the individual PDF.
- For bulk exports, select multiple rows using the checkboxes and click **Export Bulk PDF**.
- The PDF includes all employee details, assigned assets, software, signatures, and timestamps.

<br>

---

<br>

## 10. Understanding Form Statuses

Every onboarding and offboarding request moves through a series of statuses. Here is what each one means:

| Status | Display Label | What It Means |
|---|---|---|
| **Draft** | Draft | The form has been started but not yet submitted. Only the creator can see and edit it. |
| **Pending Approval** | Pending Approval | *(Offboarding)* HR has signed; a second HR user (checker) must approve. |
| **Pending HOD Approval** | Pending HOD Approval | *(Offboarding)* Waiting for the Head of Department to review and sign. |
| **Pending IT Approval** | Pending IT Approval | The form has been fully approved and is waiting for IT to provision or deprovision. |
| **Pending Employee Sign** | Pending Employee Sign | IT has completed their work; the employee must sign the acknowledgement. |
| **Completed** | Completed | All signatures collected. The process is finished. |
| **Rejected** | Rejected | The request was rejected by an approver. The rejection reason is recorded. |

> [!IMPORTANT]
> **Overdue** is not a status — it is a visual indicator. When a request exceeds the SLA target days, an "⏰ Overdue" badge appears next to the due date in red.

### Onboarding Lifecycle Diagram

```
  ┌──────────┐     ┌──────────────────┐     ┌──────────────────────┐     ┌───────────┐
  │  Draft    │ ──→ │ Pending IT       │ ──→ │ Pending Employee     │ ──→ │ Completed │
  │ (HR edits)│     │ Approval (IT)    │     │ Sign (Employee)      │     │           │
  └──────────┘     └──────────────────┘     └──────────────────────┘     └───────────┘
       │                    │
       │                    ▼
       │              ┌───────────┐
       └─────────────→│ Rejected  │
                      └───────────┘
```

### Offboarding Lifecycle Diagram

```
  ┌──────────┐    ┌──────────────┐    ┌──────────────┐    ┌──────────────┐    ┌──────────────────────┐    ┌───────────┐
  │  Draft   │──→ │  Pending     │──→ │  Pending HOD │──→ │  Pending IT  │──→ │ Pending Employee     │──→ │ Completed │
  │          │    │  Approval    │    │  Approval    │    │  Approval    │    │ Sign                 │    │           │
  └──────────┘    └──────────────┘    └──────────────┘    └──────────────┘    └──────────────────────┘    └───────────┘
                         │                   │                  │
                         ▼                   ▼                  ▼
                   ┌───────────┐       ┌───────────┐      ┌───────────┐
                   │ Rejected  │       │ Rejected  │      │ Rejected  │
                   └───────────┘       └───────────┘      └───────────┘
```

<br>

---

<br>

## 11. User Roles & Permissions

The IT Portal uses a **role-based access** system. Each user is assigned one or more roles that control what they can see and do:

| Role | What You Can Do |
|---|---|
| **HR** | Create and submit onboarding and offboarding requests. Sign off as requester. View the employee directory. Export CSV reports. Send notification emails. |
| **IT** | Perform IT setup (provisioning and deprovisioning). Manage IT assets and software licenses. Manage system users. View audit logs. Configure system settings. |
| **HOD** (Head of Department) | Sign off on offboarding requests for employees in your department. View the offboarding dashboard. |
| **Employee** (External) | Access the acknowledgement link (no portal login required) to review and sign your onboarding or offboarding form. |

> [!NOTE]
> Some users may have **multiple roles** (e.g., a person in IT who also has HR responsibilities). In that case, they see the combined menus and permissions of all assigned roles.

**What each role sees in the sidebar:**

| Menu Item | HR | IT | HOD |
|---|---|---|---|
| Dashboard | ✅ | ✅ | ✅ |
| Analytics | ✅ | ✅ | ❌ |
| Onboarding | ✅ | ✅ | ❌ |
| Offboarding | ✅ | ✅ | ✅ |
| Employees | ✅ | ✅ | ❌ |
| Email Directory | ✅ | ✅ | ✅ |
| IT Assets | ❌ | ✅ | ❌ |
| Software List | ❌ | ✅ | ❌ |
| System Users | ❌ | ✅ | ❌ |
| Audit Logs | ✅ | ✅ | ❌ |
| Due Date Setup | ❌ | ✅ | ❌ |
| Configuration | ❌ | ✅ | ❌ |

<br>

---

<br>

## 12. Configuration (IT Admin Only)

IT administrators have access to several configuration modules in the sidebar:

### Companies
Manage the list of companies/branches. Each company record includes:
- Company name and lot/address
- Company logo (displayed on the login page)
- Time zone and business hours (used for SLA calculations)
- Working days configuration (Mon–Sun)

### Departments
Add, edit, or remove departments. Departments are linked to companies and appear as options in onboarding and offboarding forms.

### Email Groups
Manage email distribution groups shown in the onboarding form (e.g., "Precast Group", "Supervisors"). These can be assigned to specific companies.

### Shared Mailboxes
Manage shared mailbox entries (e.g., "Account", "Logistic", "IT"). These appear as options during onboarding.

### Server Folders
Manage shared server folder paths. These are presented as folder permission options during onboarding.

### Due Date Setup (SLA Settings)
Configure the Service Level Agreement targets for request processing:
- **Target Days** — the expected completion timeframe (default: 3 days).
- **Tier 1 Escalation** — notification sent after X days of inactivity.
- **Tier 2 Escalation** — secondary notification sent after Y days.
- **Notification Email** — the email address that receives SLA alerts.

### System Users
Create, edit, activate, deactivate, or delete portal user accounts. Each user has:
- Username, full name, and department
- Assigned roles (HR, IT, HOD)
- Company assignments (for multi-company scoping)
- Active/Inactive status

### Audit Logs
View a full history of every action performed in the system. Features include:
- Filter by entity type, action, date range, or success/failure status
- View detailed audit entries with before/after change comparisons
- Export the audit log to CSV
- Failure analytics showing top failed actions and actors

<br>

---

<br>

## 13. Frequently Asked Questions (FAQ)

### General

**Q: I can't log in. What should I do?**
> First, ensure you are typing your username and password correctly (passwords are case-sensitive). If you've forgotten your password, click **Forgot Password?** on the login page. If your account is locked, wait for the lockout period to expire or contact your IT administrator.

**Q: I see "Access Denied" when I try to open a page. Why?**
> Your user account does not have the required role for that page. For example, only IT staff can access the IT Assets page. Contact your IT administrator to request access.

**Q: Can I use the IT Portal on my mobile phone?**
> Yes. The portal is built with a responsive design and works on smartphones and tablets. On smaller screens, the sidebar becomes a slide-out menu accessible via the ☰ (hamburger) button.

<br>

### Onboarding

**Q: What fields are required when creating an onboarding request?**
> The minimum required fields are: **Full Name**, **Department**, **Company/Branch**, and **Join Date**. All other fields are optional but highly recommended.

**Q: Can I edit an onboarding request after it has been submitted?**
> Yes, as long as the request has not yet reached the **Pending Employee Sign** or **Completed** status. Both HR and IT staff can edit submitted requests.

**Q: What happens if I save a draft instead of submitting?**
> The request is saved with a **Draft** status. You can return to it at any time from the Onboarding Dashboard and continue where you left off.

**Q: How does the employee sign if they don't have a portal account?**
> Employees do not need a portal account. They receive an email with a **unique, secure link** that opens the acknowledgement page directly in their browser. They review the details, draw their signature, and submit — no login required.

**Q: The employee lost the acknowledgement email. How can I resend it?**
> Navigate to the request's **Details** page and click the **Regenerate Token** button. This creates a new unique link (invalidating the old one). Then compose a new employee signature email.

<br>

### Offboarding

**Q: Can I link an offboarding request to a previous onboarding record?**
> Yes. When creating the offboarding request, select the employee from the **Link to Onboarding** dropdown. This pre-fills employee details and helps IT know exactly what was originally provisioned.

**Q: What is the "Checker Approval" step?**
> For offboarding, a second HR user (different from the person who created the request) must approve the request before the HOD. This provides a dual-approval safeguard.

**Q: Can I upload a resignation letter?**
> Yes. When creating the offboarding request, use the **Resignation Letter** file upload field. The file is stored securely and can be downloaded by authorised users from the request details page.

**Q: Can I reject an offboarding request?**
> Yes, any approver (HR, HOD, or IT) can reject the request at any point before it reaches the **Pending Employee Sign** stage. A rejection reason is required and an email notification is sent to the original submitter.

<br>

### IT Assets

**Q: What is an "Asset Tag"?**
> An Asset Tag is a unique label or identifier physically attached to a piece of IT equipment (e.g., "YTCP-LAP001"). It must be unique across the entire system.

**Q: Can I import assets from a spreadsheet?**
> Yes! Click **Bulk Import** on the IT Asset page. Download the provided Excel template, fill in your data, and upload. The template includes dropdown validation to help you enter data correctly.

**Q: What happens when I set an asset to "Disposed"?**
> The asset remains in the inventory for historical tracking but is no longer shown as available for assignment. Its warranty notifications are also suppressed.

**Q: What does "No Extend Warranty" mean?**
> Toggling this flag tells the system to stop sending warranty expiry reminders for that asset. Use this for assets where you have decided not to renew the warranty.

<br>

### Software & Licenses

**Q: What is the difference between "Mandatory" and non-mandatory software?**
> **Mandatory** software is automatically pre-selected on every new onboarding form. Non-mandatory software must be manually selected by HR during the onboarding process.

**Q: How do I know if I'm running out of licences?**
> Check the **Utilisation** percentage on the Software Inventory page. If you are approaching 100%, contact the vendor to purchase more seats. The system also sends automatic email alerts when licenses are about to expire.

**Q: Can I track individual licence keys?**
> Yes. Click **Manage Keys** on a licence version to add, edit, or assign individual keys. Each key can be linked to a specific employee. Keys are encrypted for security.

<br>

### Signatures & PDFs

**Q: Is my digital signature legally binding?**
> Digital signatures in this system are used for internal process tracking and accountability. Each signature is protected with a tamper-detection mechanism (HMAC). Consult your legal department for questions about legal enforceability.

**Q: Can I download a PDF of any completed form?**
> Yes. Click the 📄 PDF icon on any request in the onboarding or offboarding list. The PDF includes all employee details, assigned assets, software, checklist items, and signatures with timestamps.

<br>

---

<br>

> **Need Help?** Contact your IT Department for technical assistance or account-related queries.

<br>

*© 2026 — IT Portal System. This guide is confidential and intended for internal use only.*
