### Summary 🚀
<!-- REQUIRED: Provide a high-level summary of the changes. ---> 
Added the ability to directly purchase an item without needing to use the cart.

--------------------------------------------------------


### Related Issues 🔗
<!-- E.g., Closes #123 (You can link multiple issues, e.g., Closes #123, Fixes #456) -->
- REQUIRED: Replace this line with any related issue(s).

--------------------------------------------------------

### Breaking API Changes ⚠️

<!-- Please list any breaking changes introduced to API endpoints in this pull request. 
Explain the nature of the change and provide migration steps if applicable. (Delete this section if not applicable) -->

- Example: Removed the `GET /api/old-products` endpoint. Consumers should migrate to `GET /api/products` which now supports pagination.

- (Add more as needed)

--------------------------------------------------------

### Changes Made 💡

<!-- What was changed and why was it changed? Be detailed and include any relevant context or implementation details.--->

- Example: Refactored the `UserService` class to use constructor-based dependency injection for `IUserRepository`, improving testability and adhering to SOLID principles.

- (Add more as needed)

--------------------------------------------------------

### Developer Checklist ✅

- [ ] I have given this PR a proper name that concisely describes the overarching goal(s) of the changes made.

- [ ] I have ensured that these changes do not overreach or creep beyond the scope of the issues it aims to fix.

  - [ ] I have detailed any important concerns, observations, or out-of-scope issues in the Additional Notes section (delete this sub-point if unnecessary).

- [ ] I have added comments to my code to explain complex or non-trivial logic wherever necessary.

- [ ] I have addressed all ReSharper inspections (no errors or warnings, and suggestions considered).

  - [ ] If any ReSharper warnings were intentionally left, please explain why (delete this sub-point if no warnings are present):

- [ ] I have ensured that appropriate unit and/or integration tests have been added or updated to cover new functionality, bug fixes, and critical paths.

- [ ] I have ensured new and existing tests pass locally with my changes.

- [ ] I have made corresponding changes to any relevant documentation (if necessary).

- [ ] I have rebased my branch on the latest main branch.

- [ ] I have performed a self-review of my own code.

--------------------------------------------------------

### Additional Notes 📝
<!-- Add any other relevant information, context, or considerations for the reviewers. -->
Add notes here (delete this section if not necessary)
