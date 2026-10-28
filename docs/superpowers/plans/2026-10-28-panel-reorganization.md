# Panel Reorganization Implementation Plan

**Goal:** Apply the user's approved panel responsibilities and navy/gold visual design without losing invoices, PDFs or archive history.

**Architecture:** Keep the existing WPF shell and durable archive journal. Move shared output functionality into the invoice-list workspace. Replace the home workbench with a read-only dashboard. Remove dedicated control UI and obsolete navigation. Separate security and settings; persist three display profiles.

**Tech Stack:** .NET 8, WPF, xUnit, existing PDF services.

**Spec:** User decisions in this conversation, including approval of proposals 1–5 and corrections to proposals 6–8.

## Constraints
- Home is information only, with no buttons or navigation.
- Profiles: 1024×768, 1366×768, 1920×1080; respect DPI and available desktop work area.
- No clear buttons or step banners anywhere.
- No separate reports/control screens or dead handlers.
- Save PDF and archive package before clearing the active invoice file; rollback/preserve invoices on failure.
- HYS closure and bank payment confirmation remain distinct.

## Tasks
- [x] Add profile sizing policy and meaningful tests for DPI/work-area clamping.
- [x] Reorganize XAML navigation, dashboard, output preview, archive table, security/settings.
- [x] Remove obsolete control/workflow/clear actions; retain validation at entry/save boundaries.
- [x] Ensure packet PDF creation precedes durable archive/active-file transition.
- [x] Apply navy/gold card styles, large symbols, responsive forms and tables.
- [x] Update WPF integration tests for new navigation, read-only home, package persistence and display profiles.
- [x] Build, run tests, inspect screenshots at all three profiles and check for removed remnants.
