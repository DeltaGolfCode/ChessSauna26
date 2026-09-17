# Unit test instructions

These instructions apply to all unit tests in this repository. Tests are written in C# using **xUnit** (v3) as the test framework and **NSubstitute** for mocking/stubbing dependencies. (Mirrors `.github/instructions/unittests.instructions.md` — the Copilot custom instructions for this same directory — so tests stay consistent regardless of which assistant writes them.)

## Required NuGet Packages

The test projects must include the following approved packages:

- **NSubstitute** (6.2.0 or later) — for creating mocks and stubs of dependencies
- **xunit.v3** (4.0.1 or later) — the xUnit test framework
- **xunit.runner.visualstudio** — Visual Studio test runner integration

**Note:** The deprecated `xunit` package (previously v2.9.3 and earlier) should never be used. Always use `xunit.v3` instead.

## Structure: Arrange, Act, Assert

Every test must be organized into three clearly separated sections, marked with a comment for each:

```csharp
[Fact]
public void CalculateTotal_WhenGivenValidItems_ReturnsSumOfPrices()
{
    // Arrange
    var items = new[] { new Item(price: 10m), new Item(price: 5m) };
    var calculator = new PriceCalculator();

    // Act
    var result = calculator.CalculateTotal(items);

    // Assert
    Assert.Equal(15m, result);
}
```

- **Arrange**: construct the system under test (SUT), set up its dependencies, and configure any NSubstitute stubs.
- **Act**: invoke exactly one method or behavior on the SUT. If the act line requires more than a single statement, that's a sign the test is doing too much.
- **Assert**: verify the outcome — return value, thrown exception, state change, or an interaction with a substitute.

Never interleave arrange/act/assert logic, and never omit the comments even when a section is short.

## Naming

Use the pattern `MethodUnderTest_Scenario_ExpectedOutcome`, e.g. `Withdraw_WhenBalanceIsInsufficient_ThrowsInsufficientFundsException`. The test name should let someone understand what broke without opening the test body.

## One behavior per test

Each test verifies a single logical behavior. Prefer several small, obviously-named tests over one test with multiple unrelated assertions. Use `[Theory]` with `[InlineData]` / `[MemberData]` / `[ClassData]` to cover multiple input variations of the *same* behavior rather than copy-pasting near-identical `[Fact]`s.

## NSubstitute usage

- Create substitutes for interfaces or abstract dependencies with `Substitute.For<IDependency>()`.
- Stub return values with `substitute.Method(args).Returns(value)`. Prefer argument matchers (`Arg.Any<T>()`, `Arg.Is<T>(x => ...)`) over hardcoded values when the exact argument isn't the point of the test.
- Verify interactions with `substitute.Received(1).Method(args)` (or `DidNotReceive()`), and only when the interaction itself is the behavior under test — don't assert on interactions the test doesn't care about.
- Never substitute the class under test itself — only its dependencies.
- Avoid over-specifying stubs: only configure the members the test actually exercises.

## Assertions

- One logical concept per test; multiple `Assert` calls are fine as long as they all verify that same concept (e.g. checking several properties of a single returned object).
- Prefer precise assertions (`Assert.Equal`, `Assert.Same`, `Assert.Throws<T>`) over generic ones (`Assert.True(x == y)`).
- When asserting an exception, assert on the exception type and, where relevant, its message content:

```csharp
var exception = Assert.Throws<InsufficientFundsException>(() => sut.Withdraw(100m));
Assert.Equal("Insufficient funds for withdrawal.", exception.Message);
```

## Test independence and cleanliness

- Tests must not depend on execution order or shared mutable state. Each test creates its own SUT and substitutes; don't reuse substitutes across tests via shared fields unless reset in a constructor (xUnit creates a new test class instance per test, so constructor-based setup is safe).
- Avoid conditional logic (`if`, loops beyond simple data iteration, `try/catch` outside of exception assertions) inside a test — a test should be a straight line.
- Keep test data minimal and relevant; avoid unrelated setup that obscures what's being tested.
- Do not add comments explaining *what* the code does beyond the Arrange/Act/Assert labels — well-named variables and methods should make the test self-explanatory.
- Never use `#region`/`#endregion` blocks in test files. Regions hide code and encourage oversized test classes instead of splitting them up; if a test class feels big enough to need regions, split it into smaller, focused test classes instead.

## Coverage expectations

- Cover the happy path, boundary conditions, and failure/exception paths for public behavior.
- Test through the public API of the class under test, not its private implementation details.
- Don't write tests that merely restate the implementation (e.g. mocking every internal call and asserting the mock was called) — test observable behavior.
