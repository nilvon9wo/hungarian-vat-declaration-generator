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
		 public required string InvoiceNumber { get; init; }
		 public required decimal NetAmount { get; init; }
	 }
	 ```
   - Use `{ get; init; }` not `{ get; set; }`
   - Mark properties as `required` when mandatory

7. **No "Async" suffix on method names**
   - The return type (`Task`) and `async`/`await` keywords are sufficient
   - ❌ Avoid: `ParseAsync`, `ValidateAsync`
   - ✅ Prefer: `Parse`, `Validate`

8. **Always use Dependency Injection**
   - CRITICAL: Never create instances with `new` inside classes except for DTOs/value objects
   - Use constructor injection for all services, factories, and configuration
   - Register all dependencies in `Program.cs`
   - Even configuration objects (like `CsvConfiguration`, `JsonSerializerOptions`) should be registered as singletons and injected

### General Principles

8. **Optimize for readability over cleverness**
   - Prefer explicit code over abstract patterns
   - Code should read like well-structured prose
   - Future developers should understand intent immediately

9. **Separation of concerns**
   - Keep controllers thin (HTTP concerns only)
   - Business logic belongs in services
   - Validation should be explicit and testable

## Project-Specific Context

- This is a **coding challenge project** (60-90 minute timeframe)
- Intentionally simple: NO CQRS, NO MediatR, NO repositories, NO DDD
- Focus on working functionality over extensibility
- Production-minded but minimal

## Testing Guidelines

- Unit tests should cover business logic
- Integration tests for golden path and critical failures
- Lean testing approach (Martin Fowler's Test Pyramid)
- Test names should clearly describe what is being tested
