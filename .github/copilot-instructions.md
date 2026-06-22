# GitHub Copilot Instructions for Hungarian VAT Declaration Generator

## Code Style Requirements

### Method Size and Complexity
1. **Keep all methods under 10 lines** (strict limit)
   - If a method exceeds 10 lines, extract helper methods
   - Focus on single responsibility per method

2. **Maximum nesting depth: 2 blocks**
   - Exception: Optional 3rd block for try/catch only
   - Use early returns and guard clauses to reduce nesting
   - Extract complex conditions into well-named methods

### Code Structure Preferences

3. **Use LINQ over imperative loops**
   - Prefer LINQ to improve readability
   - Replace `for`, `foreach`, and `while` with LINQ where appropriate
   - Examples: `.Select()`, `.Where()`, `.Any()`, `.FirstOrDefault()`

4. **Method names must clearly communicate intent**
   - Use descriptive verb-noun combinations
   - Examples: `ValidateRecord`, `ParseInvoiceLine`, `MapToInvoice`
   - Avoid generic names like `Process`, `Handle`, `Do`

### C# Conventions

5. **Prefer explicit types over var**
   - ❌ Avoid: `var result = Calculate();`
   - ✅ Prefer: `VatDeclarationResult result = Calculate();`
   - Exception: When the type is obvious from the right side (e.g., `var person = new Person()`)
   - Improves code readability and makes types immediately clear

6. **Models: Use POCO syntax with init properties**
   - ❌ Avoid: `public record Invoice(string InvoiceNumber, decimal NetAmount);`
   - ✅ Prefer: 
	 ```csharp
	 public record Invoice
	 {
		 [Required]
		 public required string InvoiceNumber { get; init; }
		 [Required]
		 public required decimal NetAmount { get; init; }
	 }
	 ```
   - Use `{ get; init; }` not `{ get; set; }`
   - Mark properties as `required` when mandatory
   - **ALWAYS use both `required` keyword AND `[Required]` attribute**
   - Add validation attributes (`[Range]`, `[MinLength]`, etc.) where appropriate

7. **No "Async" suffix on method names**
   - The return type (`Task`) and `async`/`await` keywords are sufficient
   - ❌ Avoid: `ParseAsync`, `ValidateAsync`
   - ✅ Prefer: `Parse`, `Validate`

8. **Always use Dependency Injection**
   - CRITICAL: Never create instances with `new` inside classes except for DTOs/value objects
   - Use constructor injection for all services, factories, and configuration
   - Register all dependencies in `Program.cs`
   - Even configuration objects (like `CsvConfiguration`, `JsonSerializerOptions`) should be registered as singletons and injected
   - **ALWAYS validate constructor parameters are not null:**
	 ```csharp
	 private readonly IService _service = service 
		 ?? throw new ArgumentNullException(nameof(service));
	 ```
   - Use `ArgumentNullException.ThrowIfNull(parameter)` for method parameters

9. **Configuration Models**
   - Use both `required` keyword AND `[Required]` attribute on all mandatory properties
   - Add appropriate validation attributes (`[Range]`, `[MinLength]`, etc.)
   - Provide sensible defaults for optional properties
   - Example:
	 ```csharp
	 public sealed class MySettings
	 {
		 [Required]
		 [Range(1, int.MaxValue)]
		 public required int MaxItems { get; init; } = 1000;
	 }
	 ```

### General Principles

10. **Helper Method Placement - CRITICAL RULE**
    - **ALWAYS place helper methods BELOW the methods that use them**
    - **This applies to ALL code: C#, TypeScript, production, and test code**
    - ❌ **NEVER** place helpers at the top of a file
    - ✅ **DO** place helpers at the bottom, in order of use
    - **Rationale:** Readers should see the main logic first, then drill down into helpers if needed
    - **Examples:**
      - TypeScript: Public/exported functions first, then helper functions at bottom
      - C#: Public methods first, then private helper methods below
      - Tests: Test methods first, then test helper functions at bottom
      - React components: JSX return statement first, then event handlers and helpers below

11. **Optimize for readability over cleverness**
    - Prefer explicit code over abstract patterns
    - Code should read like well-structured prose
    - Future developers should understand intent immediately

12. **Separation of concerns**
    - Keep controllers thin (HTTP concerns only)
    - Business logic belongs in services
    - Validation should be explicit and testable

## Project-Specific Context

- This is a **coding challenge project** (60-90 minute timeframe)
- Intentionally simple: NO CQRS, NO MediatR, NO repositories, NO DDD
- Focus on working functionality over extensibility
- Production-minded but minimal

## Testing Guidelines

### Test Structure and Clarity

1. **Avoid "Lasagna Code" in Tests**
   - ❌ **DO NOT** hide test logic behind layers of helpers, pre-calculated constants, and indirect assertions
   - ✅ **DO** keep test logic visible and debuggable within the test method itself
   - Tests should read like a clear story: Given (inputs) → When (action) → Then (expectations)

2. **Test Values and Constants**
   - **Only extract constants for true domain values:**
     - ✅ VAT rates (e.g., `STANDARD_HUNGARIAN_VAT_RATE = 27`)
     - ✅ Invoice number patterns (e.g., `SAMPLE_INVOICE_NUMBER_001 = "INV-001"`)
     - ✅ Error messages, file extensions, MIME types, HTTP status codes
   - **DO NOT extract arbitrary test amounts:**
     - ❌ Avoid: `NET_AMOUNT_10000 = 10000m` (just verbose, no meaning)
     - ✅ Instead: Declare `decimal netAmount = 10000m;` in the test Arrange section
   - **Calculate expected values in tests:**
     - ❌ Avoid: Pre-calculated `EXPECTED_VAT = 2700m; // 10000 * 0.27` (error-prone)
     - ✅ Instead: Use helper `CalculateVat(netAmount, vatRate)` in assertions

3. **Test Helper Methods**
   - **Helpers should compute, not hide pre-calculated values**
   - ✅ **DO** create calculation helpers:
     ```csharp
     private static decimal CalculateVat(decimal netAmount, int vatRate) =>
         netAmount * vatRate / 100;
     ```
   - ❌ **DO NOT** create assertion wrappers that hide expectations:
     ```csharp
     // BAD: What is "substantial"? What values are checked?
     private static void AssertPdfIsSubstantial(byte[] pdfBytes) => ...

     // GOOD: Inline assertions show exactly what you're verifying
     Assert.NotNull(pdfBytes);
     Assert.True(pdfBytes.Length > MINIMUM_PDF_SIZE_BYTES);
     ```

4. **Test Arrangement**
   - **Declare test inputs in the Arrange section:**
     ```csharp
     // Arrange
     decimal netAmount = 10000m;
     decimal vat18Net = 8000m;
     decimal vat27Net = 18000m;

     List<Invoice> invoices = [
         new() { InvoiceNumber = "INV-001", NetAmount = netAmount, VatRate = 27 }
     ];
     ```
   - **Show calculations explicitly:**
     ```csharp
     decimal totalNet = vat18Net + vat27Net;

     Assert.Equal(totalNet, result.GrandTotalNet);
     Assert.Equal(CalculateVat(totalNet, vatRate), result.GrandTotalVat);
     ```

5. **AAA Pattern (Arrange, Act, Assert)**
   - **ALWAYS use `// Arrange`, `// Act`, `// Assert` comments**
   - **NEVER combine them:** No `// Act & Assert`
   - **Single Act:** Only one method call in the Act section
   - **Clear separation:** Each phase should be distinct and obvious

6. **Test Method Organization**
   - Public test methods first
   - Private helper methods below tests
   - Frequently-used helpers at the bottom

### Testing Philosophy

- **Tests should be immediately understandable**
  - Someone debugging at 2 AM should see inputs, action, and expectations without jumping between methods
- **Prefer directness over DRY in tests**
  - A bit of repetition is acceptable if it makes tests clearer
  - Extract only when repetition obscures meaning
- **Tests are documentation**
  - They show how the system should behave
  - They should be the easiest code to read in the project

## Testing Guidelines (Legacy - Keep for Reference)

- Unit tests should cover business logic
- Integration tests for golden path and critical failures
- Lean testing approach (Martin Fowler's Test Pyramid)
- Test names should clearly describe what is being tested
