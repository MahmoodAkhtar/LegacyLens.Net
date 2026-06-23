Yes. *Working Effectively with Legacy Code* is mainly about this problem:

> “I need to change this code, but it has little or no automated test coverage, and it is hard to test because of dependencies.”

Feathers’ central idea is that legacy code is code without tests, and the way forward is usually: **find a seam, break a dependency, get tests around the behaviour, then change/refactor safely**. The book is widely described as a set of practical strategies for bringing large untested codebases under control. ([Google Books][1])

Below is a practical catalogue of the main techniques.

| Technique                                           | Summary                                                                                                                                                                                                                                                | Why you would use it                                                                                                                                                                                          |
| --------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Characterization Tests**                          | Write tests that capture what the existing code currently does, even if the behaviour is ugly or surprising. You are not proving the code is “correct”; you are documenting its current behaviour.                                                     | Use this before refactoring. It gives you a safety net so you can tell whether you accidentally changed behaviour. This is especially useful when nobody fully understands the legacy code.                   |
| **Seams**                                           | A seam is a place where you can change behaviour without editing the code at that point. Examples include object seams, link seams, and preprocessor seams. Feathers’ seam model is one of the core ideas of the book. ([Pearson Technology Group][2]) | Use seams to replace hard dependencies during tests. For example, instead of calling a real database, file system, web service, static helper, or clock, you use a seam to substitute something controllable. |
| **Enabling Points**                                 | The place where you activate or control a seam. For example, passing in a different object, overriding a method, changing a build/link setting, or injecting a dependency.                                                                             | A seam is only useful if you can control it. The enabling point is how your test takes control of the code path.                                                                                              |
| **Sensing**                                         | Add a way to observe what the code is doing. This might mean returning a value, exposing state through a test-only path, or using a fake dependency to record calls.                                                                                   | Use this when the code does something internally but gives you no easy way to assert the result. For example, a method writes to a database but returns `void`; you need a way to “sense” what happened.      |
| **Separation**                                      | Break the code away from dependencies that stop it being tested. This often means moving logic away from framework, database, UI, file-system, or static calls.                                                                                        | Use this when the business logic is buried inside infrastructure-heavy code. Separation lets you test the important logic without needing the whole application environment.                                  |
| **Sprout Method**                                   | Add new behaviour in a new method, then call that method from the legacy method. The existing method is touched minimally.                                                                                                                             | Use this when you need to add functionality but the existing method is too risky to heavily edit. It lets new code be cleaner and more testable while leaving most legacy code unchanged.                     |
| **Sprout Class**                                    | Add new behaviour in a new class and call it from the old code.                                                                                                                                                                                        | Use this when the existing class is too large, too tangled, or too hard to instantiate. The new class can be properly designed and unit tested from the start.                                                |
| **Wrap Method**                                     | Keep the old method but wrap extra behaviour around it. For example, create a new method that does something before/after calling the original method.                                                                                                 | Use this when you need to add behaviour around an existing operation without disturbing the existing implementation too much.                                                                                 |
| **Wrap Class**                                      | Put a new class around an existing class and delegate to it, adding new behaviour at the boundary.                                                                                                                                                     | Use this when you cannot safely change the existing class directly, or when you want to start creating a cleaner API around ugly legacy behaviour.                                                            |
| **Extract Method**                                  | Move a block of logic into a separate method.                                                                                                                                                                                                          | Use this to make large methods understandable and to create smaller units that can later be tested or overridden. In legacy code, this must be done carefully, preferably after characterization tests.       |
| **Extract Class**                                   | Move a group of related responsibilities out of a large class into a new class.                                                                                                                                                                        | Use this to break up “God classes” or classes that do too many unrelated things. It helps isolate business concepts and reduces change risk.                                                                  |
| **Extract Interface**                               | Define an interface for an existing dependency so it can be substituted in tests.                                                                                                                                                                      | Use this when a class directly depends on a concrete service, repository, gateway, or infrastructure object. In C#, this is one of the most common ways to create an object seam.                             |
| **Parameterize Constructor**                        | Change a class so dependencies are passed into its constructor rather than created internally.                                                                                                                                                         | Use this when the class does `new SomeDependency()` inside itself. Constructor injection makes the dependency replaceable in tests.                                                                           |
| **Parameterize Method**                             | Pass a dependency or value into a method rather than having the method fetch or create it internally.                                                                                                                                                  | Use this for smaller, localized changes where constructor injection would be too invasive.                                                                                                                    |
| **Extract and Override Call**                       | Move a hard-to-test call into a protected virtual method, then override that method in a test subclass.                                                                                                                                                | Use this when you cannot easily introduce an interface yet. It is a tactical way to intercept calls to databases, APIs, static services, clocks, file systems, etc.                                           |
| **Extract and Override Factory Method**             | Move object creation into a virtual factory method, then override it in tests to return a fake object.                                                                                                                                                 | Use this when the code creates concrete dependencies internally and you need to replace them without rewriting the whole class.                                                                               |
| **Subclass and Override Method**                    | Create a test-specific subclass that overrides problematic behaviour.                                                                                                                                                                                  | Use this when inheritance gives you a quick seam. It can be ugly, but it is often useful as a temporary step to get tests in place.                                                                           |
| **Replace Global Reference with Getter**            | Instead of directly accessing global/static state, access it through a method or property that can be overridden or redirected.                                                                                                                        | Use this when code is tightly coupled to global state, singletons, static configuration, or environment values.                                                                                               |
| **Introduce Static Setter**                         | Add a way to replace a static dependency during tests.                                                                                                                                                                                                 | Use this when static dependencies are deeply embedded. It is not ideal design, but it can be a pragmatic stepping stone.                                                                                      |
| **Encapsulate Global References**                   | Hide global state behind a wrapper or abstraction.                                                                                                                                                                                                     | Use this to stop global state spreading through the codebase. Once wrapped, it becomes easier to replace in tests and later refactor properly.                                                                |
| **Adapt Parameter**                                 | Wrap or adapt an awkward parameter into something easier to use or test.                                                                                                                                                                               | Use this when a method takes a complex framework object, huge object graph, or hard-to-create dependency, but only uses a small part of it.                                                                   |
| **Break Out Method Object**                         | Convert a large method into a separate object whose fields represent the method’s local variables.                                                                                                                                                     | Use this when a method is too large to understand or safely refactor. Turning it into an object gives you smaller methods and more places to test.                                                            |
| **Pull Up Feature**                                 | Move common behaviour up into a base class or shared abstraction.                                                                                                                                                                                      | Use this when duplicated behaviour exists across related classes and you need one controlled place to test or change it.                                                                                      |
| **Push Down Dependency**                            | Move a dependency from a general/shared place down into the specific subclass or implementation that actually needs it.                                                                                                                                | Use this when a base class or shared component is polluted with dependencies that only some paths need. It reduces the cost of constructing and testing the general case.                                     |
| **Pinch Point**                                     | Find a narrow point in the call graph where many behaviours pass through, then put tests around that point.                                                                                                                                            | Use this when the system is too large to test everywhere. A pinch point gives you broad coverage with fewer tests.                                                                                            |
| **Effect Sketches**                                 | Draw or map what code is affected by a change. This is a reasoning tool rather than a code change.                                                                                                                                                     | Use this before touching risky code. It helps you identify where behaviour may ripple and where tests are most valuable.                                                                                      |
| **Feature Sketches**                                | Map the code paths involved in a feature.                                                                                                                                                                                                              | Use this when you need to understand how a feature works before changing it. Very useful in large legacy systems where behaviour is spread across many classes.                                               |
| **Scratch Refactoring**                             | Temporarily refactor code to understand it, without intending to commit those changes.                                                                                                                                                                 | Use this as an exploratory technique. You can learn the structure of the code, then revert and make a safer, smaller real change.                                                                             |
| **Lean on the Compiler**                            | Make small structural changes and let the compiler show you what broke.                                                                                                                                                                                | Use this when renaming, extracting, moving, or changing signatures. In statically typed languages like C#, the compiler is a powerful safety tool.                                                            |
| **Test at a Higher Level First**                    | When unit testing is too hard immediately, add broader tests around a larger slice of behaviour.                                                                                                                                                       | Use this when the code is too coupled for unit tests. Higher-level tests may be slower, but they can give you initial safety while you create better seams.                                                   |
| **Break Dependencies Before Refactoring**           | Do small dependency-breaking changes before attempting design improvements.                                                                                                                                                                            | Use this because trying to “clean up” untested code directly is dangerous. First make it testable; then improve the design.                                                                                   |
| **Make Minimal, Conservative Changes**              | Change as little as possible until tests are in place.                                                                                                                                                                                                 | Use this to reduce risk. The book’s mindset is not “big rewrite”; it is controlled, incremental change.                                                                                                       |
| **Preserve Behaviour First, Improve Design Second** | First protect existing behaviour with tests, then refactor.                                                                                                                                                                                            | Use this because legacy systems often contain hidden business rules. Even strange behaviour may be relied upon by users or other systems.                                                                     |

A very useful way to summarise the book’s workflow is:

```text
1. Identify the change you need to make.
2. Find the code affected by that change.
3. Find seams where dependencies can be broken.
4. Add characterization tests around existing behaviour.
5. Make the smallest safe change.
6. Refactor once you have feedback.
```

For your **LegacyLens.NET** thinking, the ideas that map especially well are:

| LegacyLens.NET capability idea                                         | Related Feathers technique                                                      |
| ---------------------------------------------------------------------- | ------------------------------------------------------------------------------- |
| Find high-risk methods/classes                                         | Pinch points, effect sketches, feature sketches                                 |
| Show inbound/outbound dependencies                                     | Seams, dependency breaking, effect sketches                                     |
| Identify hard dependencies like `new`, static calls, file/db/api calls | Break dependencies before refactoring                                           |
| Generate scoped dependency diagrams for a class                        | Feature sketch / effect sketch                                                  |
| Highlight untestable code areas                                        | Sensing, separation, seams                                                      |
| Suggest refactoring opportunities                                      | Sprout Class, Extract Interface, Parameterize Constructor, Extract and Override |
| Show cyclomatic complexity                                             | Helps identify code that may need characterization tests before refactoring     |

The most important practical lesson is this: **do not start by making legacy code beautiful; start by making it observable and safe to change.** Once tests exist, the design can improve incrementally.

[1]: https://books.google.com/books/about/Working_Effectively_with_Legacy_Code.html?id=fB6s_Z6g0gIC&utm_source=chatgpt.com "Working Effectively with Legacy Code - Michael Feathers"
[2]: https://ptgmedia.pearsoncmg.com/images/9780131177055/samplepages/0131177052.pdf?utm_source=chatgpt.com "Working Effectively with Legacy Code"


Yes. Martin Fowler’s *Refactoring: Improving the Design of Existing Code* overlaps with Michael Feathers’ book, but the emphasis is different.

Feathers is mainly about:

> “How do I safely change code that has no tests and is hard to test?”

Fowler is mainly about:

> “Once I have enough safety, how do I improve the internal design of existing code without changing its external behaviour?”

So Fowler’s book is more of a **catalogue of refactoring moves**. These are smaller, named transformations you apply repeatedly.

## Core idea

A refactoring is a behaviour-preserving design improvement.

You are not adding a feature. You are changing the structure so that the next feature, bug fix, or test becomes easier.

A typical Fowler-style workflow is:

```text
1. Make sure there is some safety net.
2. Make one small refactoring.
3. Run tests.
4. Make another small refactoring.
5. Keep behaviour the same.
6. Stop when the design is easier to understand or change.
```

---

# Main refactoring techniques

## 1. Extract Function / Extract Method

Move a block of code into a separate named method/function.

Before:

```csharp
public decimal CalculateInvoiceTotal(Invoice invoice)
{
    var subtotal = invoice.Lines.Sum(x => x.Quantity * x.UnitPrice);
    var tax = subtotal * 0.2m;
    return subtotal + tax;
}
```

After:

```csharp
public decimal CalculateInvoiceTotal(Invoice invoice)
{
    var subtotal = CalculateSubtotal(invoice);
    var tax = CalculateTax(subtotal);

    return subtotal + tax;
}

private decimal CalculateSubtotal(Invoice invoice) =>
    invoice.Lines.Sum(x => x.Quantity * x.UnitPrice);

private decimal CalculateTax(decimal subtotal) =>
    subtotal * 0.2m;
```

Use it when a method is doing several things, has comments explaining blocks of logic, or is hard to scan.

Why use it: it gives names to hidden concepts and makes later changes safer.

---

## 2. Inline Function / Inline Method

Replace a method call with the method body when the method adds no useful meaning.

Before:

```csharp
private bool IsEligible(Customer customer)
{
    return customer.Age >= 18;
}
```

After:

```csharp
if (customer.Age >= 18)
{
    // ...
}
```

Use it when a method is trivial, misleading, or just adds unnecessary indirection.

Why use it: too many tiny, badly named methods can make code harder to follow.

---

## 3. Extract Variable

Introduce a named local variable for part of an expression.

Before:

```csharp
if (order.Total > 1000 && order.Customer.IsPreferred && order.Items.Count > 5)
{
    ApplyDiscount(order);
}
```

After:

```csharp
var qualifiesForBulkPreferredDiscount =
    order.Total > 1000 &&
    order.Customer.IsPreferred &&
    order.Items.Count > 5;

if (qualifiesForBulkPreferredDiscount)
{
    ApplyDiscount(order);
}
```

Use it when an expression is complex or has business meaning.

Why use it: the variable name explains intent.

---

## 4. Inline Variable

Remove a variable whose name does not add value.

Before:

```csharp
var basePrice = order.BasePrice;
return basePrice;
```

After:

```csharp
return order.BasePrice;
```

Use it when the variable is noise.

Why use it: it reduces clutter.

---

## 5. Change Function Declaration

Rename a method, change parameters, or adjust its signature to better express intent.

Before:

```csharp
public decimal Calc(Customer c, bool x)
```

After:

```csharp
public decimal CalculateDiscountFor(Customer customer, bool includePromotions)
```

Use it when names or parameters hide meaning.

Why use it: good names reduce the need to inspect implementation details.

---

## 6. Rename Variable

Give a variable a clearer name.

Before:

```csharp
var d = DateTime.UtcNow;
```

After:

```csharp
var currentUtcTime = DateTime.UtcNow;
```

Use it whenever a variable’s purpose is unclear.

Why use it: naming is one of the cheapest and most powerful refactorings.

---

## 7. Encapsulate Variable

Hide direct access to a variable behind a method or property.

Before:

```csharp
public decimal Balance;
```

After:

```csharp
public decimal Balance { get; private set; }
```

Use it when public data is being changed from many places.

Why use it: it gives you one place to enforce rules.

---

## 8. Introduce Parameter Object

Replace a group of parameters with a single object.

Before:

```csharp
public IEnumerable<Order> FindOrders(
    DateTime from,
    DateTime to,
    string status,
    string customerType)
```

After:

```csharp
public IEnumerable<Order> FindOrders(OrderSearchCriteria criteria)
```

Use it when the same group of parameters appears repeatedly.

Why use it: related values become one concept.

---

## 9. Combine Functions into Class

Take several functions that operate on the same data and move them into a class.

Before:

```csharp
CalculateBasePrice(order);
CalculateDiscount(order);
CalculateTax(order);
```

After:

```csharp
var calculator = new OrderPriceCalculator(order);
calculator.CalculateTotal();
```

Use it when related behaviour is scattered across functions.

Why use it: it creates a home for behaviour and reduces duplication.

---

## 10. Combine Functions into Transform

Create a transformation step that takes input data and produces enriched output data.

Example:

```csharp
var enrichedOrder = OrderTransformer.AddPricingDetails(order);
```

Use it when many steps calculate derived values from the same source data.

Why use it: it centralises derived data logic.

---

## 11. Split Phase

Separate code into distinct phases, often parsing/collecting first and processing later.

Before:

```csharp
public Report Generate(string rawInput)
{
    // parse input
    // validate values
    // calculate result
    // render report
}
```

After:

```csharp
var parsedInput = Parse(rawInput);
var result = Calculate(parsedInput);
return Render(result);
```

Use it when one method mixes different levels of work.

Why use it: each phase becomes easier to test and reason about.

This is very relevant to LegacyLens.NET. For example:

```text
scan files -> build inventory -> analyse evidence -> produce artifacts
```

That is a split-phase design.

---

# Moving features between objects

## 12. Move Function / Move Method

Move a method to the class where it belongs better.

Before:

```csharp
public class OrderService
{
    public decimal CalculateTotal(Order order)
    {
        return order.Lines.Sum(x => x.Quantity * x.UnitPrice);
    }
}
```

After:

```csharp
public class Order
{
    public decimal CalculateTotal()
    {
        return Lines.Sum(x => x.Quantity * x.UnitPrice);
    }
}
```

Use it when a method uses another class’s data more than its own.

Why use it: behaviour should usually live near the data it works with.

---

## 13. Move Field

Move a field to the class that actually owns the concept.

Before:

```csharp
public class Customer
{
    public decimal CreditLimit;
}
```

Maybe after:

```csharp
public class CustomerAccount
{
    public decimal CreditLimit { get; set; }
}
```

Use it when data is sitting in the wrong object.

Why use it: misplaced data causes awkward dependencies and feature envy.

---

## 14. Move Statements into Function

Move repeated surrounding code into the function it belongs with.

Before:

```csharp
var price = CalculatePrice(order);
price = ApplyTax(price);
```

After:

```csharp
var price = CalculatePriceIncludingTax(order);
```

Use it when callers always perform the same extra step after calling a function.

Why use it: it prevents repeated calling conventions.

---

## 15. Move Statements to Callers

Move special-case logic out of a general function and into callers.

Before:

```csharp
public void PrintReport(Report report)
{
    PrintHeader();
    PrintBody(report);
    PrintFooter();
}
```

After:

```csharp
PrintHeader();
PrintBody(report);
PrintSpecialFooter();
```

Use it when a function has become too specific for some callers.

Why use it: it keeps general functions general.

---

## 16. Replace Inline Code with Function Call

Replace duplicated or unclear code with a call to an existing function.

Before:

```csharp
if (customer.Age >= 18)
```

After:

```csharp
if (customer.IsAdult())
```

Use it when the concept already exists elsewhere.

Why use it: it removes duplication and improves intent.

---

## 17. Slide Statements

Move related statements closer together.

Before:

```csharp
var customer = GetCustomer();
var date = DateTime.UtcNow;
var total = CalculateTotal(order);
SaveCustomer(customer);
```

After:

```csharp
var customer = GetCustomer();
SaveCustomer(customer);

var date = DateTime.UtcNow;
var total = CalculateTotal(order);
```

Use it before extracting methods.

Why use it: it groups related logic and reveals extractable blocks.

---

## 18. Split Loop

Split a loop that does multiple unrelated things into multiple loops.

Before:

```csharp
foreach (var order in orders)
{
    total += order.Total;

    if (order.IsOverdue)
    {
        overdueOrders.Add(order);
    }
}
```

After:

```csharp
foreach (var order in orders)
{
    total += order.Total;
}

foreach (var order in orders)
{
    if (order.IsOverdue)
    {
        overdueOrders.Add(order);
    }
}
```

Use it when a loop is doing multiple jobs.

Why use it: each loop has one reason to exist. Performance concerns can be handled later if proven important.

---

## 19. Replace Loop with Pipeline

Replace manual loops with collection operations such as `Where`, `Select`, `Sum`, `GroupBy`.

Before:

```csharp
var activeCustomers = new List<Customer>();

foreach (var customer in customers)
{
    if (customer.IsActive)
    {
        activeCustomers.Add(customer);
    }
}
```

After:

```csharp
var activeCustomers = customers
    .Where(customer => customer.IsActive)
    .ToList();
```

Use it when the loop is really filtering, mapping, grouping, or aggregating.

Why use it: the intent becomes clearer.

---

## 20. Remove Dead Code

Delete code that is no longer used.

Use it when code is unreachable, unused, commented out, or obsolete.

Why use it: dead code misleads developers and increases maintenance cost.

In legacy systems, this should be done carefully. First check whether reflection, configuration, plugins, or serialization depend on it.

---

# Organising data

## 21. Split Variable

Use separate variables for separate meanings.

Before:

```csharp
decimal temp = order.Total;
temp = temp * 0.2m;
temp = temp + order.Total;
```

After:

```csharp
var subtotal = order.Total;
var tax = subtotal * 0.2m;
var totalWithTax = subtotal + tax;
```

Use it when one variable is reused for different concepts.

Why use it: reused variables hide intent and make extraction harder.

---

## 22. Rename Field

Give a field a clearer domain name.

Before:

```csharp
public string Type;
```

After:

```csharp
public string CustomerCategory;
```

Use it when field names are vague or technical instead of domain-focused.

Why use it: better names reduce misunderstanding.

---

## 23. Replace Derived Variable with Query

Remove stored derived state and calculate it when needed.

Before:

```csharp
public decimal Total { get; private set; }

public void AddLine(OrderLine line)
{
    Lines.Add(line);
    Total += line.Amount;
}
```

After:

```csharp
public decimal Total => Lines.Sum(x => x.Amount);
```

Use it when a value can be derived safely from existing data.

Why use it: stored derived values can get out of sync.

---

## 24. Change Reference to Value

Turn a mutable referenced object into an immutable value object.

Example:

```csharp
public sealed record Money(decimal Amount, string Currency);
```

Use it for concepts like money, date ranges, names, IDs, coordinates, or quantities.

Why use it: value objects are easier to reason about and safer to share.

This maps strongly to your .NET/domain-driven design preferences.

---

## 25. Change Value to Reference

Use a shared reference object when identity matters.

Example:

```csharp
Customer customer = customerRepository.GetById(customerId);
```

Use it when two objects with the same ID should be treated as the same entity.

Why use it: entities need identity and lifecycle.

---

# Simplifying conditionals

## 26. Decompose Conditional

Break a complex `if` condition into named methods.

Before:

```csharp
if (date >= plan.StartDate && date <= plan.EndDate && !plan.IsSuspended)
{
    ApplyPlan(plan);
}
```

After:

```csharp
if (plan.IsActiveOn(date))
{
    ApplyPlan(plan);
}
```

Use it when conditions are hard to understand.

Why use it: business rules deserve names.

---

## 27. Consolidate Conditional Expression

Combine multiple checks that lead to the same result.

Before:

```csharp
if (customer.IsDeleted) return false;
if (customer.IsBlocked) return false;
if (!customer.HasValidEmail) return false;
```

After:

```csharp
if (customer.CannotReceiveMarketing())
{
    return false;
}
```

Use it when several conditions represent one larger concept.

Why use it: it reveals the domain rule.

---

## 28. Replace Nested Conditional with Guard Clauses

Return early for exceptional or invalid cases.

Before:

```csharp
public decimal CalculatePay(Employee employee)
{
    if (!employee.IsTerminated)
    {
        if (!employee.IsSuspended)
        {
            return employee.BasePay;
        }
    }

    return 0;
}
```

After:

```csharp
public decimal CalculatePay(Employee employee)
{
    if (employee.IsTerminated) return 0;
    if (employee.IsSuspended) return 0;

    return employee.BasePay;
}
```

Use it when nested conditionals obscure the normal path.

Why use it: the happy path becomes obvious.

---

## 29. Replace Conditional with Polymorphism

Replace `switch`/`if` chains with different classes or implementations.

Before:

```csharp
return customer.Type switch
{
    CustomerType.Standard => total,
    CustomerType.Premium => total * 0.9m,
    CustomerType.Vip => total * 0.8m,
    _ => total
};
```

After:

```csharp
public interface IDiscountPolicy
{
    decimal Apply(decimal total);
}
```

Use it when behaviour varies by type and the conditional appears in multiple places.

Why use it: adding a new type becomes adding a new class, not editing many conditionals.

Use with care. A simple switch is sometimes clearer than unnecessary polymorphism.

---

## 30. Introduce Special Case / Null Object

Replace repeated null checks or special-case checks with an object that represents the special case.

Before:

```csharp
if (customer == null)
{
    return "Guest";
}

return customer.Name;
```

After:

```csharp
return customer.Name;
```

Where `customer` may be a `GuestCustomer`.

Use it when null or special-case handling is repeated everywhere.

Why use it: it centralises special behaviour.

---

## 31. Introduce Assertion

Add an assertion to document an assumption.

Example:

```csharp
Debug.Assert(order.Total >= 0);
```

Use it when code relies on an assumption that is not obvious.

Why use it: it makes hidden assumptions visible.

---

# Refactoring APIs

## 32. Separate Query from Modifier

Split a method that both changes state and returns information.

Before:

```csharp
var customer = repository.GetCustomerAndMarkAsViewed(id);
```

After:

```csharp
var customer = repository.GetCustomer(id);
repository.MarkAsViewed(id);
```

Use it when a method has surprising side effects.

Why use it: queries should be predictable; commands should be explicit.

This is very relevant to testability.

---

## 33. Parameterize Function

Replace several similar methods with one method that takes a parameter.

Before:

```csharp
ApplyFivePercentDiscount();
ApplyTenPercentDiscount();
```

After:

```csharp
ApplyDiscount(percentage);
```

Use it when methods differ only by a value.

Why use it: removes duplication.

---

## 34. Remove Flag Argument

Replace a boolean flag with separate methods.

Before:

```csharp
Book(customer, true);
```

After:

```csharp
PremiumBook(customer);
```

Or:

```csharp
BookPremium(customer);
BookStandard(customer);
```

Use it when a boolean changes the behaviour significantly.

Why use it: `true` and `false` at call sites are unclear.

In C#, this is especially useful when you see methods like:

```csharp
Save(customer, true, false);
```

That is usually a smell.

---

## 35. Preserve Whole Object

Pass the whole object instead of extracting several values from it.

Before:

```csharp
CalculateCharge(customer.Age, customer.Region, customer.IsPreferred);
```

After:

```csharp
CalculateCharge(customer);
```

Use it when a method needs multiple values from the same object.

Why use it: it keeps related data together and makes signatures simpler.

But avoid this if it creates unnecessary coupling to a large object.

---

## 36. Replace Parameter with Query

Remove a parameter if the method can get the value itself safely.

Before:

```csharp
var basePrice = order.BasePrice;
order.CalculateDiscount(basePrice);
```

After:

```csharp
order.CalculateDiscount();
```

Use it when the parameter is always derived from the receiver.

Why use it: it reduces duplication and caller burden.

---

## 37. Replace Query with Parameter

Pass a value in instead of having the method query it.

Before:

```csharp
public decimal CalculatePrice()
{
    var exchangeRate = exchangeRateService.GetCurrentRate();
    // ...
}
```

After:

```csharp
public decimal CalculatePrice(decimal exchangeRate)
{
    // ...
}
```

Use it when hidden queries make code hard to test or unpredictable.

Why use it: it makes dependencies explicit.

This is very useful for clocks, exchange rates, current user, configuration, and environment data.

---

## 38. Remove Setting Method

Remove a setter when a value should not change after construction.

Before:

```csharp
customer.SetId(id);
```

After:

```csharp
var customer = new Customer(id);
```

Use it when mutation is unnecessary or dangerous.

Why use it: immutability reduces invalid states.

---

## 39. Replace Constructor with Factory Function

Use a factory method when object creation needs a name or extra logic.

Before:

```csharp
new Employee("engineer");
```

After:

```csharp
Employee.CreateEngineer();
```

Use it when constructors are unclear or overloaded.

Why use it: factory names can express intent better than constructor parameters.

---

## 40. Replace Function with Command

Turn a complex function into a command object.

Before:

```csharp
ProcessOrder(order, customer, pricingRules, auditLogger, clock);
```

After:

```csharp
var command = new ProcessOrderCommand(order, customer, pricingRules, auditLogger, clock);
command.Execute();
```

Use it when a function has many parameters, many steps, or needs undo/logging/state.

Why use it: it gives a complex operation its own home.

---

## 41. Replace Command with Function

Turn an unnecessary command object back into a simple function.

Use it when a class only wraps one trivial operation and carries no useful state.

Why use it: removes needless ceremony.

---

# Dealing with inheritance

## 42. Pull Up Method

Move identical methods from subclasses to a base class.

Use it when subclasses duplicate behaviour.

Why use it: removes duplication.

---

## 43. Pull Up Field

Move the same field from subclasses to a base class.

Use it when multiple subclasses store the same data.

Why use it: centralises shared state.

---

## 44. Pull Up Constructor Body

Move common constructor logic from subclasses to the base class.

Use it when constructors repeat setup code.

Why use it: keeps shared initialization in one place.

---

## 45. Push Down Method

Move a method from a base class into the subclass or subclasses that actually use it.

Use it when a base class has behaviour that does not apply to all subclasses.

Why use it: keeps abstractions honest.

---

## 46. Push Down Field

Move a field from a base class into the subclass that actually needs it.

Use it when only some subclasses use a field.

Why use it: prevents base classes becoming dumping grounds.

---

## 47. Replace Type Code with Subclasses

Replace a type field with different subclasses.

Before:

```csharp
public enum EmployeeType
{
    Engineer,
    Salesperson,
    Manager
}
```

After:

```csharp
public abstract class Employee
{
}

public sealed class Engineer : Employee
{
}

public sealed class Salesperson : Employee
{
}
```

Use it when behaviour differs significantly by type.

Why use it: type-specific behaviour can move into type-specific classes.

---

## 48. Remove Subclass

Remove subclasses that no longer justify their existence.

Use it when subclasses do not differ meaningfully.

Why use it: simpler hierarchies are easier to understand.

---

## 49. Extract Superclass

Create a base class for shared behaviour.

Use it when several classes share meaningful common behaviour.

Why use it: reduces duplication and reveals a shared abstraction.

---

## 50. Collapse Hierarchy

Merge a subclass and superclass when they are not different enough.

Use it when inheritance adds complexity without value.

Why use it: removes unnecessary abstraction.

---

## 51. Replace Subclass with Delegate

Use composition/delegation instead of inheritance.

Before:

```csharp
public class PremiumCustomer : Customer
{
}
```

After:

```csharp
public class Customer
{
    private readonly CustomerPricingPolicy pricingPolicy;
}
```

Use it when inheritance is too rigid or behaviour needs to vary independently.

Why use it: composition is often more flexible than inheritance.

---

## 52. Replace Superclass with Delegate

Remove inheritance from a superclass and delegate to another object instead.

Use it when a class inherits behaviour but does not truly have an “is-a” relationship.

Why use it: avoids misleading inheritance.

Example smell:

```csharp
public class Stack : List<object>
```

A stack is not really a general list. It may be better to contain a list internally.

---

# Data structure refactorings

## 53. Encapsulate Record

Replace direct use of loose data structures with a class.

Before:

```csharp
Dictionary<string, string> customer;
```

After:

```csharp
public class CustomerRecord
{
    public string Name { get; init; }
    public string Email { get; init; }
}
```

Use it when dictionaries, tuples, or anonymous structures are being passed around.

Why use it: named structures are safer and clearer.

---

## 54. Encapsulate Collection

Do not expose modifiable collections directly.

Before:

```csharp
public List<OrderLine> Lines { get; set; }
```

After:

```csharp
private readonly List<OrderLine> lines = new();

public IReadOnlyCollection<OrderLine> Lines => lines;

public void AddLine(OrderLine line)
{
    lines.Add(line);
}
```

Use it when callers can mutate internal state freely.

Why use it: the owning object can enforce invariants.

---

## 55. Replace Primitive with Object

Replace primitive values with a meaningful type.

Before:

```csharp
public string Email { get; set; }
public string Postcode { get; set; }
public decimal Amount { get; set; }
```

After:

```csharp
public EmailAddress Email { get; set; }
public Postcode Postcode { get; set; }
public Money Amount { get; set; }
```

Use it when primitives carry domain meaning and validation rules.

Why use it: it prevents invalid values from spreading.

This is one of the strongest refactorings for domain-heavy C# systems.

---

## 56. Replace Temp with Query

Replace a temporary variable with a method/property.

Before:

```csharp
var basePrice = quantity * itemPrice;

if (basePrice > 1000)
{
    return basePrice * 0.95m;
}

return basePrice;
```

After:

```csharp
if (BasePrice > 1000)
{
    return BasePrice * 0.95m;
}

return BasePrice;
```

Use it when a temporary variable blocks extraction or reuse.

Why use it: it makes calculations reusable and easier to move.

---

# Bigger design-level refactorings

## 57. Extract Class

Move part of a class into a new class.

Use it when a class has multiple responsibilities.

Example:

```text
Customer
  - personal details
  - billing rules
  - email notification settings
  - audit history
```

Could become:

```text
Customer
BillingAccount
NotificationPreferences
AuditTrail
```

Why use it: it reduces class size and clarifies ownership.

---

## 58. Inline Class

Merge a class back into another class when it no longer does enough.

Use it when a class has become a pointless wrapper.

Why use it: removes unnecessary structure.

---

## 59. Hide Delegate

Hide a chain of navigation behind a method.

Before:

```csharp
manager = employee.Department.Manager;
```

After:

```csharp
manager = employee.GetManager();
```

Use it when callers know too much about object internals.

Why use it: reduces coupling to internal structure.

---

## 60. Remove Middle Man

Remove a method that only delegates without adding value.

Before:

```csharp
employee.GetManager()
```

Where the method only does:

```csharp
return Department.Manager;
```

After:

```csharp
employee.Department.Manager
```

Use it when delegation becomes excessive.

Why use it: too much hiding can also make code awkward.

---

## 61. Substitute Algorithm

Replace an algorithm with a clearer one.

Use it when you find a simpler, clearer, or more standard way to do the same thing.

Before:

```csharp
// complicated hand-written searching logic
```

After:

```csharp
var match = customers.SingleOrDefault(x => x.Id == id);
```

Why use it: simpler algorithms are easier to trust.

You need tests before doing this because algorithm changes can accidentally alter edge cases.

---

# Fowler refactoring and code smells

Fowler’s refactorings are often responses to **code smells**.

| Code smell                      | Refactorings that often help                                  |
| ------------------------------- | ------------------------------------------------------------- |
| Long Method                     | Extract Function, Replace Temp with Query, Split Loop         |
| Large Class                     | Extract Class, Extract Superclass, Move Function              |
| Long Parameter List             | Introduce Parameter Object, Preserve Whole Object             |
| Duplicated Code                 | Extract Function, Pull Up Method, Substitute Algorithm        |
| Feature Envy                    | Move Function                                                 |
| Data Clumps                     | Introduce Parameter Object, Extract Class                     |
| Primitive Obsession             | Replace Primitive with Object                                 |
| Switch Statements               | Replace Conditional with Polymorphism                         |
| Lazy Class                      | Inline Class                                                  |
| Speculative Generality          | Collapse Hierarchy, Inline Class, Remove Dead Code            |
| Message Chains                  | Hide Delegate                                                 |
| Middle Man                      | Remove Middle Man                                             |
| Temporary Field                 | Extract Class                                                 |
| Global Data                     | Encapsulate Variable                                          |
| Mutable Data                    | Encapsulate Collection, Remove Setting Method, Split Variable |
| Comments explaining code blocks | Extract Function, Rename Variable                             |

---

# Difference between Fowler and Feathers

| Area             | Michael Feathers                                  | Martin Fowler                                     |
| ---------------- | ------------------------------------------------- | ------------------------------------------------- |
| Main concern     | How to safely change untested legacy code         | How to improve design without changing behaviour  |
| Starting point   | Code is hard to test                              | Code has some safety net or can be changed safely |
| Key concept      | Seams                                             | Refactorings and code smells                      |
| Main move        | Break dependencies and add characterization tests | Apply small design-preserving transformations     |
| Typical question | “How do I get this under test?”                   | “How do I make this design cleaner?”              |
| Risk level       | Very high, because tests may be missing           | Lower, assuming tests exist                       |
| Best used when   | Legacy code is hostile to testing                 | Code can already be changed with feedback         |

A good practical rule is:

```text
Use Feathers first when the code has no tests.
Use Fowler next when you have enough safety to improve the design.
```

---

# How this relates to LegacyLens.NET

For LegacyLens.NET, Fowler-style refactorings could become the basis of **refactoring opportunity detection**.

For example:

| LegacyLens.NET finding                          | Possible Fowler refactoring suggestion                                                 |
| ----------------------------------------------- | -------------------------------------------------------------------------------------- |
| Method has high cyclomatic complexity           | Extract Function, Decompose Conditional, Replace Nested Conditional with Guard Clauses |
| Class has too many responsibilities             | Extract Class                                                                          |
| Method has many parameters                      | Introduce Parameter Object, Preserve Whole Object                                      |
| Repeated dependency logic                       | Extract Function, Move Function                                                        |
| Many primitive strings/ints for domain concepts | Replace Primitive with Object                                                          |
| Public mutable collections                      | Encapsulate Collection                                                                 |
| Boolean flag parameters                         | Remove Flag Argument                                                                   |
| Large switch on type/code                       | Replace Conditional with Polymorphism                                                  |
| Duplicated code blocks                          | Extract Function, Pull Up Method                                                       |
| Class uses another class’s data heavily         | Move Function                                                                          |
| Deep chains like `a.B.C.D`                      | Hide Delegate                                                                          |
| Empty or trivial wrapper class                  | Inline Class                                                                           |
| Dead or unreachable code                        | Remove Dead Code                                                                       |
| Global/static mutable state                     | Encapsulate Variable, Replace Query with Parameter                                     |
| Inheritance hierarchy with little value         | Collapse Hierarchy, Replace Subclass with Delegate                                     |

This is where your tool idea becomes powerful.

LegacyLens.NET could eventually say something like:

```text
Finding:
CustomerService.CalculatePrice has high complexity, repeated conditional blocks,
and uses several primitive parameters.

Possible refactoring directions:
- Extract Function for discount, tax, and eligibility rules.
- Introduce Parameter Object for pricing inputs.
- Replace Primitive with Object for Money and CustomerCategory.
- Decompose Conditional for eligibility logic.
```

That would not automatically refactor the code, but it would give the developer a **guided refactoring map**.

---

# Practical combined workflow

The two books fit together very well:

```text
1. Use LegacyLens.NET to identify risky code:
   - high complexity
   - hard dependencies
   - large classes
   - static/global dependencies
   - inbound/outbound dependency hotspots

2. Use Feathers techniques:
   - find seams
   - break dependencies
   - add characterization tests
   - make code observable

3. Use Fowler techniques:
   - extract method
   - move function
   - introduce parameter object
   - replace primitive with object
   - decompose conditionals
   - simplify inheritance

4. Repeat in small steps.
```

The key distinction:

> Feathers helps you **make legacy code safe to change**.
> Fowler helps you **make safe code better designed**.


