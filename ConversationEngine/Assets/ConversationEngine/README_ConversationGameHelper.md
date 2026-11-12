# ConversationEngine.GameHelper (Runtime Evaluation and Function Execution)

ConversationEngine.GameHelper provides runtime utilities that process conversation conditions and execute function calls defined in conversation nodes.

## Project Files

### `GameHelper/ConversationEngine.GameHelper.asmdef`
Defines the assembly for runtime helper logic.
- Assembly name: `ConversationEngine.GameHelper`.
- Root namespace: `GameHelper`.
- References `ConversationEngine`, `ConversationActorBuilder`, and `ConversationEngine.System`.

### `GameHelper/ComparativeHelper.cs`
Implements condition evaluation helpers used by conversation branching.
- Parses integer, decimal, boolean, and string values.
- Evaluates `ConditionRule` entries by value type.
- Applies comparison operators (`==`, `!=`, `>`, `>=`, `<`, `<=`) for numeric values.
- Resolves variable values using game state dictionary access.

### `GameHelper/FunctionHelper.cs`
Implements runtime dynamic function invocation support.
- Looks up methods by name through reflection.
- Validates expected parameter count.
- Converts parameter values from string inputs to target method types.
- Invokes mapped methods for runtime behavior execution.

## User Guide

### 1) Evaluate conditional branches
1. Build conversation conditional nodes in the editor.
2. Ensure each branch includes valid condition rules.
3. Use runtime flow that calls `ComparativeHelper.ProcessConditions`.
4. Route conversation to the branch when all rules evaluate as true.

### 2) Execute node functions dynamically
1. Define function entries in conversation nodes (`MethodName` + `Parameters`).
2. Ensure parameter keys match the target method parameter names.
3. Run `FunctionHelper.ExecuteFunction` with the selected `ConversationFunction`.
4. Handle method lookup and conversion failures through runtime logs.

## Use Cases

- Dynamic branching based on runtime variables.
- Numeric and boolean gate checks for story progression.
- Runtime trigger execution for gameplay systems from dialogue nodes.
- Shared helper layer between conversation flow and game state systems.

## Conclusion

`ConversationEngine.GameHelper` centralizes runtime condition comparison and function invocation logic, making conversation execution more modular and easier to integrate with gameplay behavior.
