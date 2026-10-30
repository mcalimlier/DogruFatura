# Stable 1.2.0 release plan

**Goal:** Reset the entire invoice/institution card after successful save; publish stable clean distribution and deliver a separate full-data personal installer.
**Architecture:** Clear all entry selectors/fields only after durable save. Keep active invoices and school definitions intact. Derive installer version from Directory.Build.props. Retain isolated distribution profile and full personal seed bundle. Publish only verified clean EXE and its checksum to GitHub v1.2.0 (latest, not prerelease).

- [ ] Prove full reset regression in WPF, implement and run all tests.
- [ ] Remove preview suffix; derive installer names/version and verify stable metadata.
- [ ] Snapshot personal data without modifying it; build and verify both installers and first-run contents/icons.
- [ ] Push/review PR11, pass CI and merge to main; fast-forward local main for GitHub Desktop.
- [ ] Create draft release on exact merged commit, upload only clean assets, verify hashes/contents then publish stable latest.
- [ ] Provide personal installer locally, release link and verification results.
