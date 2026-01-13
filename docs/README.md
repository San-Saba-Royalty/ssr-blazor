# MineralAcquisitionWeb Application

## Comprehensive Persona & User Story Analysis

**Document Version:** 1.0
**Analysis Date:** December 19, 2025
**Application:** MineralAcquisitionWeb
**Framework:** ASP.NET Web Forms (VB.NET)

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Personas Overview](#personas-overview)
3. [Detailed Persona Profiles](#detailed-persona-profiles)
4. [Persona Relationships](#persona-relationships)
5. [User Stories by Persona](#user-stories-by-persona)
6. [Workflow Diagrams](#workflow-diagrams)
7. [Appendix: Technical Evidence](#appendix-technical-evidence)

---

## Executive Summary

### Application Purpose

MineralAcquisitionWeb is a comprehensive land/mineral rights acquisition management system used by an energy company (likely "San Saba Resources" or "SSR" based on code references) to manage the complete lifecycle of mineral rights purchases.

### Persona Count: 7 Primary Personas

| Persona              | Type     | User Story Count |
| -------------------- | -------- | ---------------- |
| System Administrator | Internal | 12               |
| Landman (Office)     | Internal | 18               |
| Landman (Field)      | Internal | 8                |
| Referrer             | External | 6                |
| Buyer                | External | 5                |
| Seller               | External | 4                |
| Attorney             | Internal | 7                |

**Total User Stories: 60**

---

## Personas Overview

### 1. System Administrator

**Goal:** Maintain system integrity, manage users, configure business rules
**Access Level:** Full administrative access
**Primary Activities:** User management, permission configuration, system setup

### 2. Landman (Office)

**Goal:** Research title, manage acquisitions, coordinate deal flow
**Access Level:** Full operational access (based on role permissions)
**Primary Activities:** Title research, acquisition management, document coordination

### 3. Landman (Field)

**Goal:** Verify property information, perform field checks
**Access Level:** Limited to field-related functions
**Primary Activities:** Field verification, check stub collection, property inspection

### 4. Referrer

**Goal:** Generate qualified leads, earn referral fees
**Access Level:** Limited view access (implied)
**Primary Activities:** Submitting deals, tracking commissions

### 5. Buyer

**Goal:** Acquire mineral rights, track investments
**Access Level:** External/limited (receives invoices and reports)
**Primary Activities:** Purchasing minerals, receiving documentation

### 6. Seller

**Goal:** Sell mineral rights, receive payment
**Access Level:** None (data subject only)
**Primary Activities:** Providing information, receiving payment

### 7. Attorney

**Goal:** Ensure legal compliance, resolve title issues
**Access Level:** Legal document access
**Primary Activities:** Title opinion, curative work, legal review

---

## Detailed Persona Profiles

### Persona 1: System Administrator

**Profile:**

- **Name/Role:** System Administrator
- **Type:** Internal Power User
- **Organizational Position:** IT/Administrative Management
- **Technical Proficiency:** High
- **Business Knowledge:** Medium-High

**Key Characteristics:**

- Has special "IsAdministrator" flag in system
- Cannot have System Administrator role modified
- Full access to all menu items and functions
- Manages other users' roles and permissions

**Goals:**

1. Ensure system availability and reliability
2. Manage user access and security
3. Configure business rules and workflows
4. Maintain reference data integrity
5. Support other users with system issues

**Pain Points:**

- Need to balance security with user accessibility
- Must understand complex permission requirements
- Responsible for data integrity across all modules
- Handle urgent user access requests

**Evidence Location:**

- `BasePage.vb` (Lines 94, 240-247, 384-386)
- `System\RoleEdit.aspx.vb` (Lines 68, 72, 132-139)
- `Account\UserEdit.aspx` (Line 169)

---

### Persona 2: Landman (Office)

**Profile:**

- **Name/Role:** Office Landman / Title Researcher
- **Type:** Internal Core User
- **Organizational Position:** Land Department
- **Technical Proficiency:** Medium-High
- **Business Knowledge:** High (title, minerals, legal)

**Key Characteristics:**

- Primary operator of acquisition workflow
- Extensive knowledge of title research
- Coordinates with attorneys and field personnel
- Manages multiple deals simultaneously
- Understanding of curative requirements

**Goals:**

1. Efficiently process mineral acquisitions
2. Identify and resolve title issues
3. Maintain accurate deal records
4. Coordinate document flow
5. Meet closing deadlines
6. Maximize deal profitability

**Pain Points:**

- Complex title chains requiring extensive research
- Coordinating multiple parties (sellers, buyers, attorneys)
- Managing curative requirements and deadlines
- Tracking numerous documents across deals
- Calculating complex financial splits

**Evidence Location:**

- `Acquisition\AcquisitionEdit.aspx` (Lines 1540-1549)
- `Acquisition\AcquisitionActions.vb` (Lines 151-210)
- Primary user of entire Acquisition module

---

### Persona 3: Landman (Field)

**Profile:**

- **Name/Role:** Field Landman
- **Type:** Internal Field User
- **Organizational Position:** Land Department (Field Operations)
- **Technical Proficiency:** Medium
- **Business Knowledge:** High (property verification)

**Key Characteristics:**

- Works primarily in the field
- Verifies physical property details
- Collects check stubs from operators
- Limited office time
- Mobile device usage (implied)

**Goals:**

1. Verify property locations accurately
2. Collect operator check stubs
3. Identify potential title issues in field
4. Provide timely field check results
5. Support office landman with ground truth

**Pain Points:**

- Remote locations with poor connectivity
- Weather and access issues
- Coordinating with operators for check stubs
- Limited system access while in field
- Need quick data entry when back in office

**Evidence Location:**

- `Acquisition\AcquisitionEdit.aspx` (Lines 1540-1549)
- Field-specific dropdown fields

---

### Persona 4: Referrer

**Profile:**

- **Name/Role:** Referrer / Deal Broker
- **Type:** External Partner
- **Organizational Position:** Independent Contractor
- **Technical Proficiency:** Low-Medium
- **Business Knowledge:** High (deal sourcing)

**Key Characteristics:**

- External independent contractor
- Commission-based compensation
- May bring multiple deals
- Requires 1099 tax reporting
- Three payment models: Seller pays, Company pays, Buyer pays

**Goals:**

1. Maximize referral fee income
2. Build pipeline of qualified deals
3. Track commission payments
4. Receive timely payments
5. Maintain good relationship with company

**Pain Points:**

- Tracking which deals are progressing
- Unclear commission calculation
- Delayed payments
- Lack of visibility into deal status
- Tax reporting complexity (1099s)

**Evidence Location:**

- `Referrer\` folder (Index, Add, Edit pages)
- `Acquisition\AcquisitionEdit.aspx` (Lines 1404-1430)
- `Report\RptReferrer1099Summary` reports
- `MenuCommand.vb` (Lines 73-74)

---

### Persona 5: Buyer

**Profile:**

- **Name/Role:** Buyer / Mineral Rights Investor
- **Type:** External Client
- **Organizational Position:** Client/Customer
- **Technical Proficiency:** Low-Medium
- **Business Knowledge:** High (investment/minerals)

**Key Characteristics:**

- External paying client
- May purchase multiple properties
- Receives invoices for acquisitions
- Has default commission percentage
- Multiple contacts per buyer organization

**Goals:**

1. Acquire quality mineral rights
2. Receive clear title
3. Understand total costs
4. Track investment portfolio
5. Receive timely documentation

**Pain Points:**

- Understanding total cost breakdown
- Tracking multiple acquisitions
- Receiving incomplete documentation
- Unclear invoice details
- Managing payment timing

**Evidence Location:**

- `Buyer\` folder (Index, Add, Edit, ContactAdd, ContactEdit)
- `Acquisition\AcquisitionActions.vb` (Lines 178-189)
- `Acquisition\AcquisitionEdit.aspx` (Lines 772-774)
- `Report\RptBuyerInvoicesDue` reports

---

### Persona 6: Seller

**Profile:**

- **Name/Role:** Seller / Mineral Rights Owner
- **Type:** External Party (Data Subject)
- **Organizational Position:** Property Owner
- **Technical Proficiency:** Low
- **Business Knowledge:** Variable

**Key Characteristics:**

- Current owner of mineral rights being acquired
- May be unsophisticated about mineral rights
- Receives bonus payment
- May pay referral fees (if applicable)
- Multiple sellers possible per acquisition

**Goals:**

1. Receive fair price for minerals
2. Understand payment breakdown
3. Receive timely payment
4. Minimize complexity
5. Get clear documentation

**Pain Points:**

- Understanding mineral rights value
- Complex legal documents
- Tax implications (Form 1099)
- Payment delays
- Referral fee deductions

**Evidence Location:**

- `Acquisition\AcquisitionEdit.aspx` (Lines 725-752, 1200-1280)
- `Acquisition\AcquisitionActions.vb` (Lines 171-176)
- Seller search functionality

---

### Persona 7: Attorney

**Profile:**

- **Name/Role:** Attorney / Legal Counsel
- **Type:** Internal Legal Staff
- **Organizational Position:** Legal Department
- **Technical Proficiency:** Medium
- **Business Knowledge:** High (title law, minerals)

**Key Characteristics:**

- Reviews legal documents
- Issues title opinions
- Resolves curative requirements
- Ensures legal compliance
- May be external counsel

**Goals:**

1. Ensure clean title transfer
2. Identify and resolve title defects
3. Minimize legal risk
4. Provide timely opinions
5. Maintain professional standards

**Pain Points:**

- Complex title chains
- Time pressure for opinions
- Coordinating curative work
- Incomplete documentation
- Multiple simultaneous deals

**Evidence Location:**

- `MainMenu.ascx` (Line 130) - Attorney document category
- `MenuCommand.vb` (Line 59) - MNU_DOCUMENTS_ATTORNEY
- Implied throughout curative workflow

---

## Persona Relationships

### Relationship Hierarchy

```
┌─────────────────────────────────────────────────────────────┐
│                  SYSTEM ADMINISTRATOR                        │
│  • Manages all users and roles                              │
│  • Configures permissions and business rules                │
└────────────────────┬────────────────────────────────────────┘
                     │
                     │ manages access for
                     │
     ┌───────────────┴───────────────┬─────────────────────────┐
     │                               │                         │
     ▼                               ▼                         ▼
┌──────────────┐              ┌──────────────┐         ┌──────────────┐
│   LANDMAN    │              │   ATTORNEY   │         │   REFERRER   │
│   (Office)   │              │              │         │  (External)  │
└──────┬───────┘              └──────┬───────┘         └──────┬───────┘
       │                             │                        │
       │ assigns field work          │ requests legal         │ brings deals
       │                             │ review/opinion         │
       ▼                             │                        │
┌──────────────┐                     │                        │
│   LANDMAN    │                     │                        │
│   (Field)    │◄────────────────────┘                        │
└──────────────┘    provides title                            │
                    issues                                     │
                                                              │
     Acquisition Workflow                                     │
     ═══════════════════                                      │
                                                              │
     ┌─────────────────────────────────────────────────┐    │
     │              ACQUISITION/DEAL                    │◄───┘
     │  • Created by Office Landman or Referrer         │
     │  • Assigned to Field Landman for verification    │
     │  • Reviewed by Attorney for title issues         │
     │  • Links to Buyer and Seller                     │
     └──────────┬───────────────────┬───────────────────┘
                │                   │
                │                   │
                ▼                   ▼
         ┌──────────────┐    ┌──────────────┐
         │    BUYER     │    │    SELLER    │
         │  (External)  │    │  (External)  │
         │              │    │              │
         │ • Receives   │    │ • Provides   │
         │   invoices   │    │   minerals   │
         │ • Pays       │    │ • Receives   │
         │   commission │    │   bonus $    │
         └──────────────┘    └──────────────┘
```

### Interaction Matrix

| From ↓ / To →        | Sys Admin       | Landman (Office) | Landman (Field) | Attorney         | Referrer       | Buyer    | Seller         |
| -------------------- | --------------- | ---------------- | --------------- | ---------------- | -------------- | -------- | -------------- |
| **Sys Admin**        | -               | Manages access   | Manages access  | Manages access   | Manages access | -        | -              |
| **Landman (Office)** | Requests access | Collaborates     | Assigns work    | Requests opinion | Receives deals | Invoices | Negotiates     |
| **Landman (Field)**  | -               | Reports to       | -               | -                | -              | -        | Verifies       |
| **Attorney**         | -               | Provides opinion | -               | -                | -              | -        | -              |
| **Referrer**         | -               | Submits leads    | -               | -                | -              | -        | Sources        |
| **Buyer**            | -               | Requests status  | -               | -                | -              | -        | Purchases from |
| **Seller**           | -               | Responds to      | -               | -                | -              | Sells to | -              |

### Workflow Handoff Sequences

#### Sequence 1: New Acquisition (Referrer-Initiated)

```
Referrer → Landman (Office) → Landman (Field) → Landman (Office) → Attorney → Landman (Office) → Buyer/Seller
```

#### Sequence 2: Direct Acquisition

```
Landman (Office) → Landman (Field) → Landman (Office) → Attorney → Landman (Office) → Buyer/Seller
```

#### Sequence 3: Curative Workflow

```
Landman (Office) → Attorney → Landman (Office) → Landman (Field) → Landman (Office)
```

---

## User Stories by Persona

### System Administrator (12 Stories)

#### Story SA-1: Create User Account

**As a** System Administrator
**I want to** create new user accounts with appropriate role assignments
**So that** new employees can access the system with proper permissions

**Inputs:**

- User full name
- Username (login ID)
- Email address
- Phone number
- Role assignment
- Administrator flag (yes/no)
- Active status

**Expected Outputs:**

- New user record created in database
- User can log in with credentials
- User has permissions based on assigned role
- Audit log entry created
- Confirmation message displayed

**Acceptance Criteria:**

- Username must be unique
- Email must be valid format
- At least one role must be assigned
- Administrator users have unrestricted access
- User appears in User Index grid

**Source:** `Account\UserAdd.aspx`, `Account\UserEdit.aspx`

---

#### Story SA-2: Manage User Roles

**As a** System Administrator
**I want to** assign and modify user roles
**So that** users have appropriate access to system functions

**Inputs:**

- User ID
- Role ID(s) to assign/remove
- Effective date (optional)

**Expected Outputs:**

- User role assignments updated
- Permission changes take effect immediately
- Audit trail of role changes
- User sees updated menu/access on next login

**Acceptance Criteria:**

- Multiple roles can be assigned to one user
- Changes are reflected in real-time
- Cannot remove all roles from active user
- System Administrator role cannot be fully edited

**Source:** `Account\UserEdit.aspx`, `System\RoleEdit.aspx.vb` (Lines 68, 72, 132-139)

---

#### Story SA-3: Configure Role Permissions

**As a** System Administrator
**I want to** define permissions for each role
**So that** users can access only the functions they need

**Inputs:**

- Role ID
- Permission codes (checkboxes)
- Permission descriptions
- Active status

**Expected Outputs:**

- Role-permission mappings saved
- Users with that role gain/lose access
- Menu items show/hide based on permissions
- Permission grid updated

**Acceptance Criteria:**

- Permissions are granular (page/function level)
- Changes affect all users with that role
- Administrator role always has all permissions
- Cannot create role without at least one permission

**Source:** `System\RoleEdit.aspx.vb` (Lines 145-162, 200-221)

---

#### Story SA-4: Create Custom Role

**As a** System Administrator
**I want to** create custom roles with specific permission sets
**So that** I can support varied business needs and organizational structures

**Inputs:**

- Role name
- Role description
- Permission selections (multiple)

**Expected Outputs:**

- New role created in system
- Role available for user assignment
- Permission matrix saved
- Success confirmation

**Acceptance Criteria:**

- Role name must be unique
- Description is required
- At least one permission must be selected
- Role appears in role dropdown lists

**Source:** `System\RoleAdd.aspx.vb`

---

#### Story SA-5: Deactivate User Account

**As a** System Administrator
**I want to** deactivate user accounts for terminated employees
**So that** former employees cannot access the system

**Inputs:**

- User ID
- Deactivation reason (optional)
- Effective date

**Expected Outputs:**

- User account marked inactive
- User cannot log in
- User's data remains in system (soft delete)
- Audit record created

**Acceptance Criteria:**

- Deactivated users shown differently in user list
- No data loss occurs
- Can reactivate if needed
- Active sessions terminated

**Source:** `Account\UserEdit.aspx` (implied)

---

#### Story SA-6: Configure Deal Statuses

**As a** System Administrator
**I want to** define custom deal status values
**So that** the workflow matches our business process

**Inputs:**

- Status name
- Status description
- Default status flag
- Exclude from reports flag
- Display order

**Expected Outputs:**

- New status available in dropdowns
- Default status auto-assigned to new deals
- Report filters respect exclude flag
- Status list updated

**Acceptance Criteria:**

- Only one default status allowed
- Status names must be unique
- Cannot delete status in use
- Order determines dropdown sequence

**Source:** `System\DealStatusIndex.aspx` (Lines 52-219)

---

#### Story SA-7: Manage Curative Types

**As a** System Administrator
**I want to** maintain the list of curative requirement types
**So that** landmen can categorize title issues consistently

**Inputs:**

- Curative type name
- Description
- Active status

**Expected Outputs:**

- Curative type available in dropdown
- Consistent categorization across deals
- Updated curative type list

**Acceptance Criteria:**

- Types must have unique names
- Cannot delete types in use
- Inactive types hidden from new entries
- Active types shown in all dropdowns

**Source:** `System\CurativeTypeIndex.aspx`

---

#### Story SA-8: Configure Lien Types

**As a** System Administrator
**I want to** define types of liens that can be recorded
**So that** lien tracking is standardized

**Inputs:**

- Lien type name
- Description
- Active flag

**Expected Outputs:**

- Lien type in dropdown menus
- Standardized lien categorization
- Updated lien type list

**Acceptance Criteria:**

- Unique lien type names
- Cannot delete types with existing liens
- Inactive types not shown for new liens

**Source:** `System\LienTypeIndex.aspx`

---

#### Story SA-9: Manage Folder Locations

**As a** System Administrator
**I want to** maintain the list of physical file locations
**So that** users can find paper files quickly

**Inputs:**

- Location code
- Location description
- Active status

**Expected Outputs:**

- Location in dropdown lists
- File tracking enabled
- Updated location list

**Acceptance Criteria:**

- Unique location codes
- Cannot delete locations in use
- Inactive locations hidden from new entries

**Source:** `System\FolderLocationIndex.aspx`

---

#### Story SA-10: View Audit Logs

**As a** System Administrator
**I want to** view system audit logs
**So that** I can track changes and troubleshoot issues

**Inputs:**

- Date range
- User filter (optional)
- Entity type filter (optional)
- Action type filter (optional)

**Expected Outputs:**

- Filtered audit log results
- Details: user, timestamp, action, before/after values
- Export capability

**Acceptance Criteria:**

- All database changes are logged
- Logs show who, what, when, before/after
- Cannot modify audit logs
- Logs retained per policy

**Source:** `Acquisition\AcquisitionAuditHistory.aspx`, `LetterAgreement\LetterAgreementAuditHistory.aspx`

---

#### Story SA-11: Configure Letter Agreement Statuses

**As a** System Administrator
**I want to** manage Letter Agreement status values separately from Acquisition statuses
**So that** pre-acquisition workflow is properly tracked

**Inputs:**

- LA Status name
- Status description
- Default flag
- Active flag

**Expected Outputs:**

- LA status available in Letter Agreement module
- Independent from Acquisition statuses
- Default applied to new LAs

**Acceptance Criteria:**

- Unique status names
- Only one default allowed
- Statuses specific to LA module

**Source:** `System\LetterAgreementDealStatusIndex.aspx`

---

#### Story SA-12: Reset User Password

**As a** System Administrator
**I want to** reset forgotten user passwords
**So that** users can regain access without delays

**Inputs:**

- User ID
- New password (or auto-generate)
- Require password change on next login flag

**Expected Outputs:**

- User password updated
- User notified (email or in-person)
- User can log in with new password
- Password change forced on next login (if flagged)

**Acceptance Criteria:**

- Password meets complexity requirements
- Audit log records password reset
- Old password immediately invalidated
- User receives secure notification

**Source:** `Account\UserEdit.aspx` (implied)

---

### Landman (Office) - 18 Stories

#### Story LO-1: Create New Acquisition

**As an** Office Landman
**I want to** create a new acquisition record
**So that** I can begin tracking a mineral rights purchase

**Inputs:**

- Acquisition ID (auto-generated or manual)
- Buyer selection
- Initial status
- Creation date (auto-populated)

**Expected Outputs:**

- New acquisition record created
- Default values populated:
    - Draft fee: $15
    - Default buyer assigned
    - Default deal status applied
    - Status date: current date
    - Created by: current user
- DocuShare collection created
- Acquisition appears in index
- Redirect to edit page

**Acceptance Criteria:**

- Acquisition ID must be unique
- Default buyer from buyer settings
- All required defaults applied
- Audit trail started
- Can immediately add details

**Source:** `Acquisition\AcquisitionActions.vb` (Lines 151-210)

---

#### Story LO-2: Search and Select Seller

**As an** Office Landman
**I want to** search for sellers by name or contact information
**So that** I can link the correct seller to an acquisition

**Inputs:**

- Seller name (partial match)
- Address (optional)
- Phone (optional)
- Email (optional)

**Expected Outputs:**

- List of matching sellers
- Seller details preview
- Ability to select seller
- Seller information populated in acquisition

**Acceptance Criteria:**

- Search supports partial matches
- Results sorted by relevance
- Can create new seller if not found
- Multiple sellers can be linked to one acquisition

**Source:** `Acquisition\AcquisitionEdit.aspx` (Lines 1200-1280)

---

#### Story LO-3: Calculate Financial Breakdown

**As an** Office Landman
**I want to** calculate total bonus, fees, and commission automatically
**So that** financial figures are accurate and consistent

**Inputs:**

- Total bonus amount
- Draft fee
- Referral fee percentage (if applicable)
- Commission percentage
- Tax amounts
- Who pays referral (Seller/SSR/Buyer)

**Expected Outputs:**

- Auto-calculated invoice total
- Gross vs. net amounts
- Tax calculations
- Commission amount
- Referral fee amount (if applicable)
- Breakdown displayed clearly

**Acceptance Criteria:**

- Formulas calculate correctly
- Referral fee splits properly
- Commission calculated on correct base
- Taxes calculated properly
- Can manually override if needed

**Source:** `Acquisition\AcquisitionEdit.aspx` (Lines 1400-1550)

---

#### Story LO-4: Assign Landman to Field Check

**As an** Office Landman
**I want to** assign a field landman to perform property verification
**So that** property details are confirmed before closing

**Inputs:**

- Acquisition ID
- Field landman selection (dropdown)
- Office landman selection (dropdown)
- Field check priority/deadline (optional)

**Expected Outputs:**

- Field landman assigned to deal
- Office landman assigned
- Assignment visible in acquisition record
- Field landman can see assignment (implied)

**Acceptance Criteria:**

- Can assign different office and field landmen
- Assignment changes logged in audit trail
- Field landman has access to deal details
- Can reassign if needed

**Source:** `Acquisition\AcquisitionEdit.aspx` (Lines 1540-1549)

---

#### Story LO-5: Manage Acquisition Units

**As an** Office Landman
**I want to** add and edit units associated with an acquisition
**So that** property boundaries and production units are properly tracked

**Inputs:**

- Unit name/identifier
- Unit type
- Counties within unit (multiple)
- Wells within unit (multiple)
- Operators per county

**Expected Outputs:**

- Unit linked to acquisition
- County associations created
- Operator-county relationships established
- Well assignments recorded
- Unit details available for reports

**Acceptance Criteria:**

- Multiple units per acquisition allowed
- Multiple counties per unit allowed
- Multiple operators per county allowed
- Relationships properly maintained
- Unit data appears in reports

**Source:** `Acquisition\AcquisitionUnitEdit.aspx`, `Acquisition\AcquisitionEdit.aspx` (Lines 1121-1127)

---

#### Story LO-6: Record Lien Information

**As an** Office Landman
**I want to** document liens against the property
**So that** title issues are tracked and resolved

**Inputs:**

- Lien type (dropdown)
- Lien amount
- Lien holder name
- Recording information
- Release status
- Notes

**Expected Outputs:**

- Lien record created
- Linked to acquisition
- Visible in lien summary
- Included in title work review

**Acceptance Criteria:**

- Multiple liens per acquisition allowed
- Lien types from configured list
- Can mark as released
- Lien details in reports

**Source:** `Acquisition\AcquisitionLienEdit.aspx`

---

#### Story LO-7: Create Curative Requirement

**As an** Office Landman
**I want to** document title defects that need resolution
**So that** curative work can be tracked and assigned

**Inputs:**

- Curative type (dropdown)
- Description of issue
- Priority/severity
- Assigned to (attorney, landman)
- Due date
- Resolution notes

**Expected Outputs:**

- Curative requirement created
- Linked to acquisition
- Visible in curative tracking grid
- Assignee can see requirement
- Status tracking enabled

**Acceptance Criteria:**

- Multiple curatives per acquisition
- Can assign to different personnel
- Status updates tracked
- Completion date recorded
- Visible in curative reports

**Source:** `Acquisition\AcquisitionCurativeEdit.aspx`

---

#### Story LO-8: Upload Acquisition Documents

**As an** Office Landman
**I want to** upload documents related to an acquisition
**So that** all related files are centrally stored and accessible

**Inputs:**

- Document file (PDF, Word, etc.)
- Document type/category (dropdown)
- Document description
- Upload date (auto)
- Uploaded by (auto)

**Expected Outputs:**

- Document uploaded to DocuShare
- Document linked to acquisition
- Document appears in document list
- Document searchable
- Document retrievable

**Acceptance Criteria:**

- Multiple documents per acquisition
- Categorized by type
- Size limits enforced
- Virus scanning performed
- Version control supported

**Source:** `Acquisition\AcquisitionDocumentUpload.aspx`, `Acquisition\AcquisitionDocuments.aspx`

---

#### Story LO-9: Generate Barcode Cover Sheet

**As an** Office Landman
**I want to** print barcode cover sheets for documents
**So that** scanned documents are automatically linked to acquisitions

**Inputs:**

- Acquisition ID
- Document category
- Number of copies

**Expected Outputs:**

- PDF cover sheet with barcode
- Barcode encodes acquisition ID + category
- Printable format
- Ready for scanner integration

**Acceptance Criteria:**

- Barcode scans correctly
- Acquisition ID encoded properly
- Professional appearance
- Multiple copies supported

**Source:** `Acquisition\AcquisitionDocumentBarcode.aspx`, `Acquisition\AcquisitionDocumentBarcodeMultiple.aspx`

---

#### Story LO-10: Add Acquisition Notes

**As an** Office Landman
**I want to** add notes to an acquisition
**So that** important communications and decisions are documented

**Inputs:**

- Note text
- Note category/type (optional)
- Date/time (auto)
- Author (auto)
- Private/public flag

**Expected Outputs:**

- Note saved to acquisition
- Note visible to authorized users
- Note timestamped
- Note attributed to author
- Note in chronological order

**Acceptance Criteria:**

- Unlimited notes per acquisition
- Cannot edit others' notes (optional)
- Can attach documents to notes
- Notes searchable
- Notes in audit history

**Source:** `Acquisition\AcquisitionNotes.aspx`

---

#### Story LO-11: Copy Existing Acquisition

**As an** Office Landman
**I want to** create a new acquisition by copying an existing one
**So that** I can save time on similar deals

**Inputs:**

- Source acquisition ID
- Fields to copy (selections)
- New acquisition ID

**Expected Outputs:**

- New acquisition created
- Selected fields copied
- New unique ID assigned
- Financial fields reset/cleared
- Status reset to default
- Audit trail started fresh

**Acceptance Criteria:**

- Does not copy financial transactions
- Does not copy status history
- Copies master data (buyer, contacts)
- Creates new DocuShare collection
- User confirms before copying

**Source:** `Acquisition\AcquisitionCopy.aspx`

---

#### Story LO-12: Manage Referrer Assignment

**As an** Office Landman
**I want to** assign a referrer to an acquisition and configure fee split
**So that** referral fees are calculated and paid correctly

**Inputs:**

- Referrer selection (dropdown)
- Referral fee percentage
- Who pays referral (Seller/SSR/Buyer)
- Referral fee notes

**Expected Outputs:**

- Referrer linked to acquisition
- Fee percentage saved
- Payment responsibility recorded
- Fee calculation updated
- Referrer can see deal (implied)

**Acceptance Criteria:**

- Only one referrer per acquisition
- Fee percentage validates (0-100%)
- Payment option required if referrer assigned
- Fee included in financial calculations
- Referrer in 1099 reports

**Source:** `Acquisition\AcquisitionReferrerEdit.aspx`, `Acquisition\AcquisitionEdit.aspx` (Lines 1404-1430)

---

#### Story LO-13: Update Acquisition Status

**As an** Office Landman
**I want to** change the status of an acquisition
**So that** deal progress is tracked through the workflow

**Inputs:**

- Acquisition ID
- New status (dropdown)
- Status change date (auto or manual)
- Status change notes (optional)

**Expected Outputs:**

- Status updated in acquisition record
- Status history record created
- Status date updated
- User ID and timestamp recorded
- Visible in audit history

**Acceptance Criteria:**

- Status change logged with reason
- Previous status preserved in history
- Status progression validated (optional)
- Reports reflect new status immediately

**Source:** `Acquisition\AcquisitionEdit.aspx`, `Acquisition\AcquisitionActions.vb` (Lines 192-202)

---

#### Story LO-14: Search Acquisitions

**As an** Office Landman
**I want to** search for acquisitions using multiple criteria
**So that** I can quickly find specific deals

**Inputs:**

- Acquisition ID (partial)
- Buyer name
- Seller name
- County
- Operator
- Status
- Date range
- Landman assignment

**Expected Outputs:**

- List of matching acquisitions
- Summary details per result
- Sortable columns
- Exportable results
- Link to full acquisition record

**Acceptance Criteria:**

- Multiple search criteria supported
- Results paginated for performance
- Search is fast (<3 seconds)
- Can save search criteria
- Results show key summary data

**Source:** `Acquisition\AcquisitionIndex.aspx`, `Acquisition\AcquisitionIndex2.aspx`, `Acquisition\AcquisitionIndexCurrent.aspx`

---

#### Story LO-15: Generate Draft Due Report

**As an** Office Landman
**I want to** run a report of outstanding drafts
**So that** I can follow up on pending payments

**Inputs:**

- Date range
- Buyer filter (optional)
- Status filter (optional)
- County filter (optional)

**Expected Outputs:**

- Report showing:
    - Acquisition ID
    - Seller name
    - Buyer name
    - Draft amount
    - Draft date
    - Days outstanding
- Subtotals by buyer/county
- Exportable to Excel/PDF

**Acceptance Criteria:**

- Report shows only unpaid drafts
- Aging calculations correct
- Sortable by multiple columns
- Can drill down to acquisition details

**Source:** `Report\RptDraftsDue\` folder

---

#### Story LO-16: Generate Buyer Invoice Report

**As an** Office Landman
**I want to** run buyer invoice reports
**So that** buyers can be billed for acquisitions

**Inputs:**

- Buyer selection
- Date range
- Invoice status (paid/unpaid)
- County filter (optional)

**Expected Outputs:**

- Report showing:
    - Acquisition details
    - Invoice amounts
    - Commission calculations
    - Payment status
- Summary by buyer
- Exportable format

**Acceptance Criteria:**

- Commission calculated correctly
- Includes all line items
- Groupable by buyer
- Shows payment status

**Source:** `Report\RptBuyerInvoicesDue\` folder

---

#### Story LO-17: Link Letter Agreement to Acquisition

**As an** Office Landman
**I want to** convert a Letter Agreement into a full Acquisition
**So that** preliminary deals can advance to closing

**Inputs:**

- Letter Agreement ID
- Conversion confirmation
- Additional acquisition details

**Expected Outputs:**

- New acquisition created
- LA data copied to acquisition
- LA marked as "converted" or locked
- Link established between LA and acquisition
- User redirected to acquisition edit

**Acceptance Criteria:**

- LA fields map to acquisition fields
- Financial data transferred correctly
- Cannot convert same LA twice
- LA remains viewable after conversion

**Source:** `Acquisition\AcquisitionActions.vb` (Lines 234-239)

---

#### Story LO-18: View Acquisition Audit History

**As an** Office Landman
**I want to** see the complete change history of an acquisition
**So that** I can understand who changed what and when

**Inputs:**

- Acquisition ID
- Date range filter (optional)
- User filter (optional)
- Field filter (optional)

**Expected Outputs:**

- Chronological list of changes
- Details: field name, old value, new value, user, timestamp
- Filterable and sortable
- Exportable

**Acceptance Criteria:**

- All field changes captured
- User and timestamp on every change
- Cannot modify audit history
- Searchable by field name
- Includes status changes

**Source:** `Acquisition\AcquisitionAuditHistory.aspx`

---

### Landman (Field) - 8 Stories

#### Story LF-1: View Assigned Field Checks

**As a** Field Landman
**I want to** see a list of acquisitions assigned to me for field verification
**So that** I can prioritize my field work

**Inputs:**

- User login (auto)
- Status filter (pending, complete)
- Date range
- Priority sort

**Expected Outputs:**

- List of assigned acquisitions
- Property addresses
- Priority indicators
- Due dates
- Contact information

**Acceptance Criteria:**

- Only shows my assignments
- Updates in real-time
- Sortable by priority/date
- Shows location/map (optional)

**Source:** Implied from field assignment functionality

---

#### Story LF-2: Update Field Check Status

**As a** Field Landman
**I want to** mark an acquisition as "field check complete"
**So that** the office knows verification is done

**Inputs:**

- Acquisition ID
- Field check status (Yes/No/N/A)
- Field check date (auto or manual)
- Field notes

**Expected Outputs:**

- Field check status updated
- Office landman notified (optional)
- Timestamp recorded
- Notes saved

**Acceptance Criteria:**

- Status immediately visible to office
- Cannot mark complete without notes
- Date defaults to today
- Can upload photos (optional)

**Source:** `Acquisition\AcquisitionEdit.aspx` (Lines 1511-1549)

---

#### Story LF-3: Record Check Stub Collection

**As a** Field Landman
**I want to** indicate whether I collected check stubs from operators
**So that** payment tracking is accurate

**Inputs:**

- Acquisition ID
- Check stub status (Yes/No/N/A)
- Stub collection date
- Operator confirmation
- Notes

**Expected Outputs:**

- Check stub status updated
- Visible in acquisition record
- Operator payment tracking enabled

**Acceptance Criteria:**

- Status options: Yes/No/N/A
- Date recorded
- Can link scanned stub images
- Operator contact recorded

**Source:** `Acquisition\AcquisitionEdit.aspx` (Lines 1511-1549)

---

#### Story LF-4: Add Field Notes

**As a** Field Landman
**I want to** add notes about field observations
**So that** office landman has context for title work

**Inputs:**

- Acquisition ID
- Note text (observations, issues, photos)
- Note date/time (auto)
- GPS location (optional)

**Expected Outputs:**

- Field note added to acquisition
- Timestamped and attributed
- Visible to office landman
- Photos attached (if applicable)

**Acceptance Criteria:**

- Notes tagged as "field notes"
- Can attach multiple photos
- GPS location captured if available
- Office landman receives notification

**Source:** `Acquisition\AcquisitionNotes.aspx`

---

#### Story LF-5: Identify Property Access Issues

**As a** Field Landman
**I want to** document property access problems
**So that** the office can coordinate with landowners

**Inputs:**

- Acquisition ID
- Access issue type (locked gate, no road, etc.)
- Description
- Contact information needed
- Resolution needed

**Expected Outputs:**

- Access issue flagged
- Office notified
- Issue tracked to resolution
- Contact information requested

**Acceptance Criteria:**

- Issue categories predefined
- High priority flag available
- Can request office assistance
- Issue appears in reports

**Source:** Implied from field workflow

---

#### Story LF-6: Verify Property Boundaries

**As a** Field Landman
**I want to** confirm property boundaries match legal description
**So that** acquisition covers the correct parcel

**Inputs:**

- Acquisition ID
- Legal description (reference)
- Field observations
- Boundary markers found
- Discrepancies noted

**Expected Outputs:**

- Verification status updated
- Discrepancies flagged for office
- Photos of markers attached
- Survey needs identified

**Acceptance Criteria:**

- Can mark as "verified" or "issues found"
- Discrepancies require description
- Photos linked to report
- Survey recommendation recorded

**Source:** Implied from field verification workflow

---

#### Story LF-7: Record Operator Contact Information

**As a** Field Landman
**I want to** update operator contact details from field visits
**So that** office has current information

**Inputs:**

- Operator ID
- Contact name
- Phone number
- Email address
- Physical address
- Office hours
- Notes

**Expected Outputs:**

- Operator contact updated
- Change logged in audit trail
- Available to office immediately
- Contact verification date recorded

**Acceptance Criteria:**

- Updates reflect immediately
- Old contact info preserved in history
- Verification date tracked
- Can add multiple contacts

**Source:** `Operator\OperatorContactEdit.aspx`

---

#### Story LF-8: Submit Mileage and Expenses

**As a** Field Landman
**I want to** record mileage and expenses per acquisition
**So that** I can be reimbursed accurately

**Inputs:**

- Acquisition ID
- Mileage (miles)
- Expense amounts
- Expense types
- Receipt attachments
- Date of expense

**Expected Outputs:**

- Expense record created
- Linked to acquisition
- Submitted for approval
- Appears in expense report
- Reimbursement processed

**Acceptance Criteria:**

- Multiple expenses per acquisition
- Receipts required over threshold
- Mileage calculated by rate
- Approval workflow enabled

**Source:** Implied from business requirements (not explicitly in code reviewed)

---

### Referrer - 6 Stories

#### Story RF-1: Submit New Deal Opportunity

**As a** Referrer
**I want to** submit mineral rights opportunities to the company
**So that** I can generate referral fee income

**Inputs:**

- Property/seller information
- Location details
- Estimated value
- Contact information
- How lead was sourced
- Urgency/timeline

**Expected Outputs:**

- Lead record created
- Company notified of new lead
- Tracking number assigned
- Status: "Pending Review"
- Referrer can see submission

**Acceptance Criteria:**

- Minimum required fields enforced
- Confirmation sent to referrer
- Company reviews within SLA
- Duplicate checking performed

**Source:** Implied from referrer workflow

---

#### Story RF-2: View My Active Deals

**As a** Referrer
**I want to** see the status of deals I referred
**So that** I can track my pipeline and expected commissions

**Inputs:**

- Referrer login (auto)
- Status filter (all, active, closed, cancelled)
- Date range

**Expected Outputs:**

- List of referred acquisitions
- Current status per deal
- Expected referral fee
- Payment status
- Last updated date

**Acceptance Criteria:**

- Shows only my deals
- Real-time status updates
- Estimated fee visible
- Payment status clear

**Source:** Implied from referrer relationship to acquisitions

---

#### Story RF-3: Update Referrer Profile

**As a** Referrer
**I want to** update my contact and payment information
**So that** the company can reach me and pay me correctly

**Inputs:**

- Contact name
- Company name (if applicable)
- Address
- Phone
- Email
- Tax ID / SSN (for 1099)
- Payment method preference
- W-9 form upload

**Expected Outputs:**

- Profile updated
- Tax information secured
- Payment routing updated
- 1099 information correct

**Acceptance Criteria:**

- Tax ID required for payments
- W-9 required annually
- Contact changes immediate
- Payment info validated

**Source:** `Referrer\ReferrerEdit.aspx`

---

#### Story RF-4: View Commission Statement

**As a** Referrer
**I want to** see a detailed commission statement
**So that** I understand how my fees are calculated

**Inputs:**

- Referrer login (auto)
- Acquisition ID (optional)
- Date range

**Expected Outputs:**

- Commission breakdown:
    - Total bonus
    - Referral fee percentage
    - Fee amount
    - Who is paying (Seller/SSR/Buyer)
    - Payment date
- Year-to-date total
- Paid vs. unpaid amounts

**Acceptance Criteria:**

- Calculation transparent
- Shows payment responsibility
- Historical statements available
- Matches 1099 amounts

**Source:** Implied from referral fee calculation

---

#### Story RF-5: Receive 1099 Summary

**As a** Referrer
**I want to** receive an annual 1099 tax summary
**So that** I can file my taxes correctly

**Inputs:**

- Tax year
- Referrer ID (auto)

**Expected Outputs:**

- IRS Form 1099-MISC or 1099-NEC
- Total fees paid in tax year
- Breakdown by quarter
- Company tax information
- Electronic and paper options

**Acceptance Criteria:**

- Matches IRS requirements
- Issued by January 31
- Accessible in portal
- Mailed to address on file
- Includes all applicable deals

**Source:** `Report\RptReferrer1099Summary\` folder

---

#### Story RF-6: Dispute Fee Calculation

**As a** Referrer
**I want to** submit a dispute about my fee calculation
**So that** errors can be corrected

**Inputs:**

- Acquisition ID
- Expected fee amount
- Calculated fee amount
- Explanation of discrepancy
- Supporting documentation

**Expected Outputs:**

- Dispute ticket created
- Company reviews dispute
- Resolution communicated
- Adjustment made if warranted
- Updated statement issued

**Acceptance Criteria:**

- Dispute tracked to resolution
- Response within SLA
- Adjustment retroactive if needed
- Explanation provided

**Source:** Implied from business process

---

### Buyer - 5 Stories

#### Story BY-1: View My Acquisitions

**As a** Buyer
**I want to** see all acquisitions I'm purchasing
**So that** I can track my mineral rights portfolio

**Inputs:**

- Buyer login (auto)
- Status filter
- Date range
- County filter

**Expected Outputs:**

- List of all acquisitions for this buyer
- Summary information:
    - Acquisition ID
    - Property location
    - Seller name
    - Total cost
    - Status
    - Closing date
- Exportable to Excel

**Acceptance Criteria:**

- Shows only my acquisitions
- Real-time status updates
- Sortable and filterable
- Summary totals displayed

**Source:** Implied from buyer relationship

---

#### Story BY-2: Receive Invoice

**As a** Buyer
**I want to** receive detailed invoices for acquisitions
**So that** I know exactly what to pay

**Inputs:**

- Acquisition ID
- Buyer ID (auto)
- Invoice date

**Expected Outputs:**

- Detailed invoice showing:
    - Total bonus to seller
    - Commission amount
    - Commission percentage
    - Referral fees (if buyer pays)
    - Taxes
    - Total amount due
- Payment instructions
- Due date
- PDF format

**Acceptance Criteria:**

- All line items detailed
- Commission calculated correctly
- Payment terms clear
- Professional invoice format

**Source:** `Report\RptBuyerInvoicesDue\` folder, `Acquisition\AcquisitionEdit.aspx` (Lines 772-774)

---

#### Story BY-3: Access Acquisition Documents

**As a** Buyer
**I want to** download documents related to my acquisitions
**So that** I have complete records

**Inputs:**

- Acquisition ID
- Document category filter (optional)

**Expected Outputs:**

- List of all documents for acquisition
- Document metadata (type, date, description)
- Download capability
- Organized by category

**Acceptance Criteria:**

- Only shows buyer-relevant documents
- Secure access control
- Multiple document formats supported
- Download tracking (optional)

**Source:** Implied from document management workflow

---

#### Story BY-4: Update Buyer Profile

**As a** Buyer
**I want to** update my company and contact information
**So that** communications and payments are sent correctly

**Inputs:**

- Company name
- Primary contact name
- Billing address
- Mailing address (if different)
- Phone number
- Email address
- Default commission percentage
- Payment terms preference

**Expected Outputs:**

- Buyer profile updated
- Changes reflected in new acquisitions
- Default commission applied to future deals
- Contact information current

**Acceptance Criteria:**

- Commission percentage validates (0-100%)
- Contact changes immediate
- Can add multiple contacts
- Billing vs. mailing addresses distinct

**Source:** `Buyer\BuyerEdit.aspx`

---

#### Story BY-5: View Purchase History Report

**As a** Buyer
**I want to** run reports on my acquisition history
**So that** I can analyze my investment activity

**Inputs:**

- Date range
- County filter (optional)
- Operator filter (optional)
- Status filter

**Expected Outputs:**

- Summary report showing:
    - Total acquisitions
    - Total invested
    - Total acres/minerals
    - Average cost per acre
    - County breakdown
    - Timeline of purchases
- Charts/graphs (optional)
- Exportable format

**Acceptance Criteria:**

- Calculations accurate
- Historical data complete
- Visual representations clear
- Export to Excel/PDF

**Source:** `Report\RptPurchases\` folder

---

### Seller - 4 Stories

#### Story SL-1: Provide Contact Information

**As a** Seller
**I want to** provide my contact and payment information
**So that** I can receive payment for my minerals

**Inputs:**

- Full legal name
- Mailing address
- Phone number
- Email address
- Tax ID / SSN (for 1099)
- Payment preference (check, wire, ACH)
- W-9 form

**Expected Outputs:**

- Seller record created or updated
- Information secured
- Payment routing established
- Tax documentation on file

**Acceptance Criteria:**

- Required fields enforced
- Tax ID secured (encrypted)
- W-9 required for payments >$600
- Payment info validated

**Source:** `Acquisition\AcquisitionEdit.aspx` (Lines 725-752, 1200-1280)

---

#### Story SL-2: Receive Payment Breakdown

**As a** Seller
**I want to** understand how my payment is calculated
**So that** I know what deductions were made

**Inputs:**

- Acquisition ID
- Seller ID

**Expected Outputs:**

- Payment breakdown showing:
    - Gross bonus amount
    - Draft fee (if applicable)
    - Referral fee (if seller pays)
    - Tax withholding (if applicable)
    - Net payment amount
- Payment date
- Check/wire number

**Acceptance Criteria:**

- All deductions explained
- Matches check amount
- Tax reporting accurate
- Professional format

**Source:** Implied from financial calculations

---

#### Story SL-3: Receive 1099 Tax Form

**As a** Seller
**I want to** receive my 1099 for mineral rights sale
**So that** I can file my taxes

**Inputs:**

- Tax year
- Seller ID

**Expected Outputs:**

- IRS Form 1099-MISC or 1099-S
- Total payment received
- Company tax information
- Mailed by January 31
- Electronic copy (optional)

**Acceptance Criteria:**

- Matches IRS requirements
- Timely delivery
- Correct tax classification
- Matches actual payments

**Source:** Implied from tax reporting requirements

---

#### Story SL-4: Update Payment Information

**As a** Seller
**I want to** update my payment address or method
**So that** I receive future payments correctly

**Inputs:**

- Updated address
- Updated payment method
- Effective date
- Confirmation

**Expected Outputs:**

- Payment information updated
- Change confirmed
- Future payments routed correctly
- Old information preserved in history

**Acceptance Criteria:**

- Changes verified before applying
- Can specify effective date
- Notification sent to company
- Audit trail maintained

**Source:** Implied from seller management

---

### Attorney - 7 Stories

#### Story AT-1: Review Assigned Curative Requirements

**As an** Attorney
**I want to** see all curative requirements assigned to me
**So that** I can prioritize title defect resolution

**Inputs:**

- Attorney login (auto)
- Status filter (pending, in progress, resolved)
- Priority filter
- Due date filter

**Expected Outputs:**

- List of assigned curatives
- Details:
    - Acquisition ID
    - Curative type
    - Issue description
    - Priority
    - Due date
- Sortable by priority/date
- Link to acquisition details

**Acceptance Criteria:**

- Shows only my assignments
- Real-time updates
- Indicates overdue items
- Links to related documents

**Source:** `Acquisition\AcquisitionCurativeEdit.aspx`

---

#### Story AT-2: Issue Title Opinion

**As an** Attorney
**I want to** upload and record my title opinion
**So that** the acquisition can proceed or issues can be addressed

**Inputs:**

- Acquisition ID
- Title opinion document (PDF)
- Opinion type (clean, requires curative, reject)
- Issue date
- Summary/notes
- Exceptions listed

**Expected Outputs:**

- Opinion document uploaded
- Linked to acquisition
- Status updated based on opinion type
- Landman notified
- Curative requirements created (if applicable)

**Acceptance Criteria:**

- Opinion document required
- Opinion type drives workflow
- Exceptions itemized
- Landman receives notification
- Searchable by opinion type

**Source:** Implied from attorney workflow

---

#### Story AT-3: Request Additional Documentation

**As an** Attorney
**I want to** request additional documents from the landman
**So that** I can complete my title review

**Inputs:**

- Acquisition ID
- Document type(s) needed
- Reason/justification
- Urgency level
- Due date

**Expected Outputs:**

- Request sent to assigned landman
- Request tracked in acquisition
- Landman receives notification
- Status: "Pending Additional Docs"

**Acceptance Criteria:**

- Request details clear
- Landman notified immediately
- Can specify multiple doc types
- Tracks response time

**Source:** Implied from document workflow

---

#### Story AT-4: Resolve Curative Requirement

**As an** Attorney
**I want to** mark a curative requirement as resolved
**So that** acquisition can move to closing

**Inputs:**

- Curative requirement ID
- Resolution description
- Supporting documents
- Resolution date
- Confirmed by

**Expected Outputs:**

- Curative marked resolved
- Resolution details saved
- Landman notified
- Acquisition status may auto-update
- Documents linked

**Acceptance Criteria:**

- Resolution description required
- Supporting docs mandatory
- Cannot re-resolve
- Appears in audit trail

**Source:** `Acquisition\AcquisitionCurativeEdit.aspx`

---

#### Story AT-5: Generate Attorney Document from Template

**As an** Attorney
**I want to** generate legal documents from templates
**So that** I can efficiently produce consistent documentation

**Inputs:**

- Template selection (deed, affidavit, etc.)
- Acquisition ID (auto-populates data)
- Custom fields
- Signature requirements

**Expected Outputs:**

- Document generated with populated fields
- Acquisition data merged correctly
- Professional formatting
- Ready for review/signature
- Saved to acquisition documents

**Acceptance Criteria:**

- Templates professionally formatted
- Data merges accurately
- Can save as draft
- Version control maintained

**Source:** `MainMenu.ascx` (Line 130), `MenuCommand.vb` (Line 59)

---

#### Story AT-6: Track Curative Workload

**As an** Attorney
**I want to** see my curative workload metrics
**So that** I can manage my time and commitments

**Inputs:**

- Attorney ID (auto)
- Date range
- Status filter

**Expected Outputs:**

- Dashboard showing:
    - Total assigned
    - Resolved this period
    - Pending resolution
    - Average resolution time
    - Overdue items
- Trend charts
- Drill-down capability

**Acceptance Criteria:**

- Real-time data
- Historical comparison
- Export capability
- Shareable with management

**Source:** `Report\RptCurativeRequirements\` folder

---

#### Story AT-7: Review Acquisition Legal Documents

**As an** Attorney
**I want to** access all legal documents for an acquisition
**So that** I can perform thorough title review

**Inputs:**

- Acquisition ID
- Document category (Attorney, Seller, etc.)

**Expected Outputs:**

- Filtered list of legal documents
- Document metadata
- Preview capability
- Download capability
- Organized chronologically

**Acceptance Criteria:**

- Shows only relevant categories
- Fast document loading
- Multiple format support (PDF, Word, etc.)
- Annotation capability (optional)

**Source:** Implied from document management system

---

## Workflow Diagrams

### Primary Acquisition Workflow

```
┌─────────────────────────────────────────────────────────────────┐
│                    ACQUISITION WORKFLOW                          │
└─────────────────────────────────────────────────────────────────┘

[START: New Deal Opportunity]
        │
        ├──[Source: Referrer]──────────┐
        │                              │
        └──[Source: Direct]             │
                │                       │
                ▼                       ▼
        ┌────────────────┐      ┌──────────────┐
        │ Create Letter  │      │  Referrer    │
        │   Agreement    │      │ Submits Deal │
        │   (optional)   │      └──────┬───────┘
        └───────┬────────┘             │
                │                      │
                ▼                      ▼
        ┌────────────────────────────────────┐
        │ CREATE ACQUISITION RECORD          │
        │ • Auto-assign default buyer        │
        │ • Set default status               │
        │ • Create DocuShare collection      │
        │ • Assign Acquisition ID            │
        └───────────────┬────────────────────┘
                        │
                        ▼
        ┌────────────────────────────────────┐
        │ ENTER ACQUISITION DETAILS          │
        │ • Seller information               │
        │ • Property/unit details            │
        │ • Financial terms                  │
        │ • Referrer assignment (if any)     │
        └───────────────┬────────────────────┘
                        │
                        ▼
        ┌────────────────────────────────────┐
        │ ASSIGN FIELD LANDMAN               │
        │ • Select field landman             │
        │ • Select office landman            │
        │ • Set field check priority         │
        └───────────────┬────────────────────┘
                        │
                        ▼
        ┌────────────────────────────────────┐
        │ FIELD VERIFICATION                 │
        │ • Property inspection              │
        │ • Boundary verification            │
        │ • Operator check stub collection   │
        │ • Field notes added                │
        └───────────────┬────────────────────┘
                        │
                        ▼
        ┌────────────────────────────────────┐
        │ TITLE RESEARCH                     │
        │ • Office landman reviews title     │
        │ • Identify liens                   │
        │ • Identify curative requirements   │
        │ • Upload title documents           │
        └───────────────┬────────────────────┘
                        │
                        ├──[Issues Found]────────────┐
                        │                            │
                        │                            ▼
                        │                ┌──────────────────────┐
                        │                │ CREATE CURATIVE      │
                        │                │ REQUIREMENTS         │
                        │                │ • Assign to attorney │
                        │                │ • Set priority       │
                        │                └──────────┬───────────┘
                        │                           │
                        │                           ▼
                        │                ┌──────────────────────┐
                        │                │ ATTORNEY REVIEW      │
                        │                │ • Issue opinion      │
                        │                │ • Resolve curatives  │
                        │                │ • Request add'l docs │
                        │                └──────────┬───────────┘
                        │                           │
                        │                           │
                        │◄──────[Resolved]──────────┘
                        │
                        ▼
        ┌────────────────────────────────────┐
        │ FINANCIAL CALCULATION              │
        │ • Calculate total bonus            │
        │ • Calculate referral fee (if any)  │
        │ • Calculate commission             │
        │ • Calculate taxes                  │
        │ • Generate invoice                 │
        └───────────────┬────────────────────┘
                        │
                        ▼
        ┌────────────────────────────────────┐
        │ DOCUMENT PREPARATION               │
        │ • Generate barcode sheets          │
        │ • Upload closing documents         │
        │ • Attorney documents               │
        │ • Buyer/Seller documents           │
        └───────────────┬────────────────────┘
                        │
                        ▼
        ┌────────────────────────────────────┐
        │ CLOSING                            │
        │ • Update status: "Closed"          │
        │ • Record payment information       │
        │ • Record check/draft numbers       │
        │ • Finalize documents               │
        └───────────────┬────────────────────┘
                        │
                        ▼
        ┌────────────────────────────────────┐
        │ POST-CLOSING                       │
        │ • Generate buyer invoice           │
        │ • Track payments                   │
        │ • Generate 1099s (year-end)        │
        │ • Archive documents                │
        └────────────────────────────────────┘
                        │
                        ▼
                  [END: Deal Complete]
```

### Curative Resolution Sub-Workflow

```
┌─────────────────────────────────────────────────────────────────┐
│                 CURATIVE RESOLUTION WORKFLOW                     │
└─────────────────────────────────────────────────────────────────┘

[Curative Requirement Identified]
        │
        ▼
┌────────────────────────────────────┐
│ LANDMAN CREATES CURATIVE          │
│ • Select curative type             │
│ • Describe issue                   │
│ • Assign to attorney               │
│ • Set priority and due date        │
└───────────────┬────────────────────┘
                │
                ▼
┌────────────────────────────────────┐
│ ATTORNEY NOTIFIED                  │
│ • Receives assignment notification │
│ • Reviews curative details         │
│ • Accesses related documents       │
└───────────────┬────────────────────┘
                │
                ├──[Needs More Info]────────┐
                │                           │
                │                           ▼
                │                 ┌───────────────────┐
                │                 │ REQUEST DOCUMENTS │
                │                 │ • List needed docs│
                │                 │ • Set due date    │
                │                 └─────────┬─────────┘
                │                           │
                │                           ▼
                │                 ┌───────────────────┐
                │                 │ LANDMAN RESPONDS  │
                │                 │ • Uploads docs    │
                │                 │ • Notifies attorney│
                │                 └─────────┬─────────┘
                │                           │
                │◄──────────────────────────┘
                │
                ▼
┌────────────────────────────────────┐
│ ATTORNEY RESEARCHES SOLUTION       │
│ • Legal research                   │
│ • Prepare curative documents       │
│ • Draft affidavits, deeds, etc.    │
└───────────────┬────────────────────┘
                │
                ├──[External Action Needed]──────┐
                │                                 │
                │                                 ▼
                │                     ┌────────────────────┐
                │                     │ COORDINATE W/ 3RD  │
                │                     │ PARTIES            │
                │                     │ • Sellers          │
                │                     │ • Other lienholders│
                │                     │ • County clerk     │
                │                     └──────────┬─────────┘
                │                                │
                │◄───────────────────────────────┘
                │
                ▼
┌────────────────────────────────────┐
│ EXECUTE CURATIVE SOLUTION          │
│ • Record documents                 │
│ • Obtain releases                  │
│ • Update title status              │
└───────────────┬────────────────────┘
                │
                ▼
┌────────────────────────────────────┐
│ ATTORNEY MARKS RESOLVED            │
│ • Upload resolution documents      │
│ • Document resolution details      │
│ • Update curative status           │
│ • Notify landman                   │
└───────────────┬────────────────────┘
                │
                ▼
┌────────────────────────────────────┐
│ LANDMAN VERIFIES RESOLUTION        │
│ • Reviews resolution               │
│ • Confirms title clear             │
│ • Updates acquisition status       │
└───────────────┬────────────────────┘
                │
                ▼
        [Curative Complete]
                │
                └──►[Acquisition Proceeds to Closing]
```

### Role-Based Access Control Flow

```
┌─────────────────────────────────────────────────────────────────┐
│              ROLE-BASED ACCESS CONTROL FLOW                      │
└─────────────────────────────────────────────────────────────────┘

[User Logs In]
        │
        ▼
┌─────────────────────────┐
│ AUTHENTICATION          │
│ • Username/Password     │
│ • Credentials validated │
└───────────┬─────────────┘
            │
            ▼
┌─────────────────────────┐
│ LOAD USER PROFILE       │
│ • User ID               │
│ • User Full Name        │
│ • IsAdministrator flag  │
│ • Assigned Role(s)      │
└───────────┬─────────────┘
            │
            ├──[IsAdministrator = TRUE]───────────┐
            │                                     │
            │                                     ▼
            │                         ┌──────────────────────┐
            │                         │ GRANT FULL ACCESS    │
            │                         │ • All menu items     │
            │                         │ • All functions      │
            │                         │ • All modules        │
            │                         └──────────┬───────────┘
            │                                    │
            │                                    │
            ▼                                    │
┌─────────────────────────┐                     │
│ LOAD ROLE PERMISSIONS   │                     │
│ • Query role-permission │                     │
│   mapping for user's    │                     │
│   assigned role(s)      │                     │
└───────────┬─────────────┘                     │
            │                                    │
            ▼                                    │
┌─────────────────────────┐                     │
│ BUILD PERMISSION SET    │                     │
│ • List of permission    │                     │
│   codes user has access │                     │
│   to (e.g., "ACQ_EDIT", │                     │
│   "RPT_VIEW")           │                     │
└───────────┬─────────────┘                     │
            │                                    │
            ▼                                    │
┌─────────────────────────┐                     │
│ RENDER MENU             │                     │
│ • Show only menu items  │                     │
│   user has permission   │                     │
│   to access             │                     │
│ • Hide restricted items │                     │
└───────────┬─────────────┘                     │
            │                                    │
            └────────────────┬───────────────────┘
                             │
                             ▼
                [User Sees Personalized Interface]
                             │
        ┌────────────────────┴────────────────────┐
        │                                         │
        ▼                                         ▼
[User Navigates to Page]            [User Attempts Action]
        │                                         │
        ▼                                         ▼
┌─────────────────────────┐          ┌─────────────────────────┐
│ PAGE LOAD PERMISSION    │          │ ACTION PERMISSION CHECK │
│ CHECK                   │          │ (e.g., Delete button)   │
│ • Check if user has     │          │ • Verify permission     │
│   permission for page   │          │   before executing      │
└───────────┬─────────────┘          └───────────┬─────────────┘
            │                                     │
            ├──[Permission Denied]────┐           ├──[Permission Denied]────┐
            │                         │           │                         │
            │                         ▼           │                         ▼
            │              ┌────────────────┐     │              ┌────────────────┐
            │              │ REDIRECT TO    │     │              │ SHOW ERROR MSG │
            │              │ ACCESS DENIED  │     │              │ "Insufficient  │
            │              │ PAGE           │     │              │ Permissions"   │
            │              └────────────────┘     │              └────────────────┘
            │                                     │
            ▼                                     ▼
    [Page Loads]                           [Action Executed]
            │                                     │
            └────────────────┬────────────────────┘
                             │
                             ▼
                    [User Continues Work]
```

---

## Appendix: Technical Evidence

### File Structure Evidence

- **Total Files Analyzed:** 40+ key files across modules
- **Total Lines of Code Reviewed:** ~15,000 lines
- **Modules Examined:** 8 major modules

### Code References by Module

#### Acquisition Module

- `Acquisition\AcquisitionEdit.aspx` (2,877 lines) - Primary acquisition management
- `Acquisition\AcquisitionActions.vb` (Lines 71-241) - Business logic
- `Acquisition\AcquisitionIndex.aspx` - Acquisition search/list
- `Acquisition\AcquisitionCopy.aspx` - Copy functionality
- `Acquisition\AcquisitionNotes.aspx` - Notes management
- `Acquisition\AcquisitionDocuments.aspx` - Document management
- `Acquisition\AcquisitionAuditHistory.aspx` - Audit trail
- `Acquisition\AcquisitionUnitEdit.aspx` - Unit management
- `Acquisition\AcquisitionLienEdit.aspx` - Lien tracking
- `Acquisition\AcquisitionCurativeEdit.aspx` - Curative management
- `Acquisition\AcquisitionReferrerEdit.aspx` - Referrer assignment

#### User/Security Module

- `Account\UserIndex.aspx` - User listing
- `Account\UserAdd.aspx` - User creation
- `Account\UserEdit.aspx` (Line 169) - User modification
- `Account\UserInfoEdit.aspx` - User profile
- `Account\UserLogin.aspx` - Authentication
- `System\RoleIndex.aspx` - Role listing
- `System\RoleAdd.aspx` - Role creation
- `System\RoleEdit.aspx.vb` (Lines 12-45, 68-72, 132-139, 145-162, 200-221) - Role/Permission management
- `BasePage.vb` (Lines 94, 240-247, 384-386) - Base security logic

#### Letter Agreement Module

- `LetterAgreement\LetterAgreementIndex.aspx`
- `LetterAgreement\LetterAgreementEdit.aspx`
- `LetterAgreement\LetterAgreementCopy.aspx`
- `LetterAgreement\LetterAgreementNotes.aspx`
- `LetterAgreement\LetterAgreementDocuments.aspx`
- `LetterAgreement\LetterAgreementAuditHistory.aspx`

#### Entity Management Modules

- `Buyer\BuyerEdit.aspx` - Buyer management
- `Buyer\BuyerContactAdd.aspx` / `BuyerContactEdit.aspx`
- `Referrer\ReferrerEdit.aspx` - Referrer management
- `County\CountyIndex.aspx` - County management
- `Operator\OperatorIndex.aspx` - Operator management
- `Operator\OperatorCheckStatement.aspx` - Check stub tracking

#### System Configuration

- `System\DealStatusIndex.aspx` (Lines 52-219) - Deal status configuration
- `System\LetterAgreementDealStatusIndex.aspx` - LA status configuration
- `System\LienTypeIndex.aspx` - Lien type management
- `System\CurativeTypeIndex.aspx` - Curative type management
- `System\FolderLocationIndex.aspx` - Physical location tracking

#### Reporting Module

- `Report\RptDraftsDue\` - Draft payment tracking
- `Report\RptBuyerInvoicesDue\` - Buyer invoicing
- `Report\RptPurchases\` - Purchase history
- `Report\RptCurativeRequirements\` - Curative tracking
- `Report\RptLetterAgreementDeals\` - LA tracking
- `Report\RptReferrer1099Summary\` - Referrer tax reporting

#### Menu/Navigation

- `MainMenu.ascx` (Lines 107-138) - Menu structure
- `MenuCommand.vb` (Lines 11, 20, 48, 59, 64, 73-74) - Menu command definitions

### Database Entity Evidence

Based on code analysis, the following entities were identified:

**Primary Entities:**

- Acquisition
- LetterAgreement
- User
- Role
- Permission
- Buyer
- Seller
- Referrer
- County
- Operator
- Unit
- Lien
- CurativeRequirement
- Document
- Note
- AuditHistory

**Relationship Entities:**

- AcquisitionSeller
- AcquisitionReferrer
- AcquisitionUnit
- AcquisitionCounty
- AcquisitionOperator
- AcquisitionLien
- AcquisitionCurative
- UserRole
- RolePermission

### Permission Codes Identified

While complete list not available in reviewed files, examples include:

- `MNU_*` - Menu access permissions
- `ACQ_*` - Acquisition module permissions
- `RPT_*` - Report access permissions
- `SYS_*` - System administration permissions

---

## Document Change History

| Version | Date       | Author               | Changes                        |
| ------- | ---------- | -------------------- | ------------------------------ |
| 1.0     | 2025-12-19 | Claude (AI Analysis) | Initial comprehensive analysis |

---

**End of Report**