# Record Dialogs and Archive Row Actions

**Goal:** Replace inline school editing and archive detail cards with themed, cancel-safe dialogs; show all donut percentages and copyable version information.

## Tasks
- School New/Edit dialogs keep changes local until Save; validate and persist atomically; show success notification only after successful save.
- Move New/Edit into school toolbar card; enable Edit only with selection; reclaim editor space for table.
- Add HYS action on each archive row. Pending Save validates HYS and commits closure; Cancel/X commits nothing.
- Closed packet Edit transfers through the existing transaction journal to an empty active list and removes the closed packet. A nonempty active list blocks all changes and informs the user.
- Remove standalone history button and selected-list card; include packet information/history in action dialog; preserve PDF opening and deletion on row.
- Render every positive donut slice percentage, including very small ones, with non-overlapping labels.
- Separate short version and release channel; allow mouse selection/Ctrl+C on metadata and values.
- Test school save/cancel, archive save/cancel, duplicate HYS, blocked reopen, successful reopen and failure rollback; check three screen profiles and screenshots.
