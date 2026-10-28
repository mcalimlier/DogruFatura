# Archive Cards, Institution Resolution And Two Installers

> **For agentic workers:** Execute inline using superpowers:executing-plans.

**Goal:** Apply the user's seven requested refinements and deliver two clearly distinguished self-contained Windows installation executables.

**Architecture:** Resolve institution identities using current school IDs, unambiguous service numbers and verified name aliases. Use the existing WPF theme and NSIS toolchain. Existing user data must never be replaced by installer defaults. User subsequently authorized correcting persisted archive names and deleting this maintenance operation's temporary backup after verification.

**Tech Stack:** .NET 8, WPF, xUnit, NSIS, PowerShell.

**Spec:** User's seven-item request in this conversation. Four side-by-side navy/gold archive cards, matching slice colors with endpoint dots, centered school sheet, explain preview channel, personal school-only and empty distribution setup.

## Constraints And Review Focus

- Never remove the legitimate numeric prefix in a name such as 31 Ağustos or 30 Ağustos by a generic regex.
- Ambiguous shared service numbers must not merge separate school campuses.
- Statistics, filters, drilldowns, comparison and exports must use the same identity projection without changing totals or archive seals.
- School selection must support Turkish case and substring search and remain keyboard usable.
- User revised the personal installer to include schools, signatories, active invoices, archive, document PDFs, preferences and audit/journal records. Public setup has no embedded private defaults, initial data bundle, invoices, archive, signatures, logs or backup data.
- Installation/update/uninstall preserve existing user files; no forced termination of an open application.

## Tasks

- [x] Add regression tests for numeric-name aliases, service matching, ambiguous services, reporting totals and pristine first-run variants; observe failures before fixes.
- [x] Implement institution identity projection and import normalization; use it consistently in reports.
- [x] Implement filtered editable institution picker and verify 31 Ağustos selection.
- [x] Replace archive description block with four themed cards and optional details/history disclosure.
- [x] Match callouts to slice colors with endpoint dots; center the school sheet and apply thin navy separators.
- [x] Build and test all relevant behavior; capture and inspect screenshots.
- [x] Build self-contained app and two NSIS setups with explicit titles and filenames; validate extracted contents and isolated first runs; record hashes and verify real user-data hashes unchanged.

## Verified Data Maintenance

User authorized persistent correction and subsequent removal of this operation's backup. `scripts/ArchiveMaintenance` dry run identified 23 name corrections in 14 packages, zero unmatched names, zero duplicate school names. Applied with old/new counts and totals verified, all closed snapshot hashes verified; retained 115 packages, 363 invoices, 27 schools. All 118 protected school/active/signature/PDF file hashes remained unchanged. Temporary archive backup deleted after success. Full details remain in ignored `artifacts/institution-repair-applied.json`, not in either installer.

## Subsequent Approved Changes

Archive cards show packet/HYS, stamp duty, payment, VAT. Active invoices require exit confirmation even when already persisted. User removed the preview badge and reduced chrome to 36×30 with 11.5pt glyphs. Installer production was paused for user review, then explicitly resumed. Personal installer was revised from school-only to full current operational data; first-run bootstrap validates schema, PDF hashes and bundle manifest before staging, preserves existing/partial user folders, and rolls back its own moved directories on failure. No backups or crash logs are bundled.

## Final Verification

200 tests passed in `artifacts/full-installer-tests.log`. Both final EXEs were extracted by their own installers with `/S /VERIFYONLY` into isolated workspaces; package SHA-256, titles, private-default exclusion in the shared core, engine self-test, first-run bootstrap, full record/PDF hashes and existing-data preservation passed. Personal first run: 27 schools, 115 archives / 363 archived invoices, signatories present, zero active invoices in the captured snapshot. Public first run: zero schools/invoices/archives and no signatories or seed bundle. All 123 protected real-user files retained their original SHA-256. Superseded school-only EXE removed. Final artifacts and hashes: `DAGITIM/1.2.0-preview.1/`; verification evidence: `DOGRULAMA.json`. No signing certificate was configured; both EXEs are unsigned.
