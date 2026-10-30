# Water recipient and cross-PC review implementation plan

**Goal:** Belediye su faturalarında KDV/damga sıfır ve tam ödeme; şirket faturalarında standart hesap.
**Architecture:** Persist an optional recipient on invoices and school defaults. An unspecified legacy value retains historical calculations and is omitted from JSON to preserve sealed archive hashes. New UI entries explicitly select municipality or company; municipality disables and zeros VAT. Reopening restores the invoice choice, not the current school default.
**Stack:** .NET 8, WPF, xUnit.

- [x] Add failing calculation, persistence and legacy snapshot compatibility tests.
- [x] Implement recipient-aware calculation/validation; avoid applying legacy water migration to company invoices.
- [x] Add themed invoice selector and school default; restore edit state, dirty tracking and totals.
- [x] Review all 11 commits on ui/responsive-card-layout-20261027. Port compatible shared visual refinements; preserve current responsive profiles, compact typography and removed-panel decisions.
- [x] Run focused and full tests/build; review diff for legacy compatibility. Do not mutate live user data or rebuild release assets as part of this change.

## Cross-PC reconciliation

Fetched all remote branches on 2026-10-30. `main` remains at `1b18dbc` (published preview); the other-PC work is `ui/responsive-card-layout-20261027` at `36e0401`, based on older `25b18fd`.

| Commits | Review / resolution |
| --- | --- |
| fc36c98, e2a1312 | Old fixed Viewbox/downscale sizing and startup ResponsiveLayoutService conflict with current Grid reflow and explicit three display profiles. Retain the newer reflow. |
| f4d2c6d, 57a58ee, 8bbd540 | Shared navy/gold KPI cards already superseded by PaymentMetricCard/PaymentCardEdge and vector symbols on all current dashboard cards. Retain newer shared resources, avoid duplicate unused styles and bitmap shadows on text. |
| 2f33900 | Large gold navigation symbols: port gold foreground to current compact symbols without enlarging sidebar widths. |
| 33b4254 | Port premium gradient and rounded active/hover navigation styling; keep compact height and padding. |
| b3d0074, 8c4d4ce | Retain newer compact typography; port layout rounding and stronger shared panel border. Old 21pt headings reverse approved smaller sizing. |
| 13fd3c7, 36e0401 | Current financial/archival/statistics cards already share navy/gold PaymentMetricCard. Do not revert to cream cards or resurrect removed Report/Control panels. |

No branch reset, force push or user-data edits. Other-PC commits remain intact on their remote branch.

## Verification
Full suite: 210 passed. After the final dynamic policy-label correction, focused calculation/persistence/WPF suite: 6 passed. WPF render screenshots checked at 1024x768 and 1366x768; all three profiles exercised. Company payment 1200 / VAT 200 = stamp 9.48, payment 1190.52. Municipality = VAT/stamp zero, payment 1200. Legacy serialization omits unspecified recipient; sealed archive roundtrip and tamper detection pass. Live data and release binaries untouched.
