# Installer Quality And Profile Isolation

**Goal:** Empty distribution with usable onboarding, separate personal data, correct Windows icons and a readable branded installer.

**Evidence:** Installed distribution core returns empty authority defaults. Existing desktop personal profile is present; both variants previously used that same root. XAML also retained personal sample labels. NSIS welcome title combined version and a long variant name in a fixed-height control; uninstall registration omitted DisplayIcon.

**Design:** Use a distribution.profile installation marker and a central runtime path policy. Distribution gets its own desktop data root and application/shortcut/uninstall identity; personal keeps its established data root. Never delete existing records to make a distribution look empty. No automatic legacy migration into distribution. Show one brief startup notice for missing institutions/signatories. Remove personal sample XAML text, disable misleading ready status for empty signatories. Use short installer captions, navy/gold branded images, readable body text and explicit icon registration.

## Work

- [x] Regression coverage for marker/path separation, shared-store defaults, onboarding, empty UI preview and personal preservation.
- [x] Route stores, PDF, safety and logs through the profile policy; keep explicit test paths intact.
- [x] Add onboarding and empty signatory UI.
- [x] Brand NSIS, shorten titles, register desktop and uninstall icons, separate installer identities.
- [x] Test full app and both installers with isolated first runs; inspect live installer page text/flow and brand artwork.
- [x] Replace both delivered EXEs, verify hashes and real user data preservation.

## Verification

205 tests passed (`artifacts/installer-quality-tests.log`). Both delivered installers passed extraction, embedded-icon extraction, actual .lnk target/icon checks, and the shared uninstall registration routine using temporary test registry keys (`DOGRULAMA.json`). Public runtime marker and all default store/PDF paths were checked. Personal first run retained 27 schools, 115 archives and signatories. Public first run had zero schools/invoices/archives, empty signatories, and onboarding notice.

The actual published distribution application was started on this PC while original personal data remained present: responsive main window, zero dashboard counts, and the requested First Use information dialog were observed through native accessibility state. Physical distribution data files were confirmed empty (`artifacts/distribution-runtime-result.json`). All 123 protected personal data/PDF files retained their original hashes. The disposable empty-profile runtime process was stopped after the check.

Native screenshot capture returned black images on this desktop session, so no pixel-perfect native-window screenshot claim is made. Installer control text/flow was inspected live; branded assets were viewed directly, and long captions were replaced with short one/two-line headings within the standard MUI controls. Both final EXEs and their hash manifest were regenerated.
