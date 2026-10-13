# Dual-Model Verification Workflow ("Route, Check, Verify")

## Overview
AssetDesk enforces a dual-model governance discipline across all development modules:
1. **Route:** Select the appropriate model for implementation based on domain constraints and architecture specifications.
2. **Check:** A secondary, independent model audits the git diff against `AGENTS.md` and `docs/ARCHITECTURE.md`.
3. **Verify:** Disputed claims, edge cases, and reviewer findings are checked against runtime execution evidence.

## The Case Study: Reviewer Classification Contradiction
During an integer division verification exercise (`RouterDivisionTest`), an AI code reviewer flagged an overflow edge case:
- **Input:** `int.MinValue / -1` (`-2147483648 / -1`)
- **Reviewer Label:** "Unhandled Edge Case / Defect"
- **Reviewer Explanation:** "The division results in 2147483648, which exceeds 32-bit signed integer capacity. This will throw an OverflowException, falling into the generic catch (Exception ex) block and printing 'Unexpected error: Arithmetic operation resulted in an overflow.'"

### Runtime Verification
Execution in .NET 8 demonstrated:
- The `catch (Exception ex)` block caught the exception cleanly.
- The output matched the reviewer's prediction word-for-word.
- **Conclusion:** The exception was handled. The reviewer accurately modeled execution behavior while assigning an erroneous taxonomy label.

## Invariants for AI-Assisted Engineering
1. **Never Trust Labels Over Mechanics:** LLMs frequently predict runtime behavior accurately while misapplying defect categories.
2. **Evidence Over Prompts:** Verify claims against compiler diagnostics, `git show --stat`, and terminal execution logs rather than re-prompting.
3. **Multi-Model Separation:** Never allow the implementing model to act as its own unassisted reviewer.
