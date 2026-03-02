---
disable-model-invocation: true
---

Adds a transactional email template using Fluid (Liquid).

Each email has a 3-file pattern plus a model record.

## Steps

**1. Create the model record** in `src/backend/MyProject.Application/Features/Email/Models/EmailTemplateModels.cs`:

```csharp
/// <summary>
/// Model for the order-confirmation template. Exposed as <c>order_number</c> and <c>total</c> in Liquid.
/// </summary>
public record OrderConfirmationModel(string OrderNumber, string Total);
```

Properties auto-map to snake_case Liquid variables (`OrderNumber` -> `order_number`).

**2. Create 3 template files** in `src/backend/MyProject.Infrastructure/Features/Email/Templates/`:

- `order-confirmation.liquid` - HTML body fragment (injected into `_base.liquid` via `{{ body | raw }}`):
  ```html
  <h2 style="margin: 0 0 20px 0; font-size: 22px; font-weight: 700; color: #333333;">Order Confirmed</h2>
  <p style="margin: 0 0 16px 0;">Your order <strong>{{ order_number }}</strong> totaling {{ total }} has been confirmed.</p>
  ```
- `order-confirmation.subject.liquid` - Subject line (plain text, no HTML)
- `order-confirmation.text.liquid` - Plain text body (optional but recommended)

All marked as **EmbeddedResource** (verify `.csproj` has `<EmbeddedResource Include="Features\Email\Templates\*.liquid" />`).

**3. Register the model type** in `FluidEmailTemplateRenderer.CreateOptions()`:

```csharp
options.MemberAccessStrategy.Register<OrderConfirmationModel>();
```

**4. Use in a service:**

```csharp
await templatedEmailSender.SendSafeAsync("order-confirmation",
    new OrderConfirmationModel(order.Number, order.FormattedTotal),
    customer.Email, cancellationToken);
```

**5. Add tests** in `FluidEmailTemplateRendererTests.cs` - test subject rendering, HTML variable injection, and plain text variant.

**6. Verify:** `dotnet test src/backend/MyProject.slnx -c Release`

## Conventions

- Template names: kebab-case (`order-confirmation`, not `orderConfirmation`)
- HTML templates: inline styles (email clients ignore `<style>` blocks in fragments)
- `_base.liquid` provides the wrapper (header with `{{ app_name }}`, card, footer) - fragments provide inner content only
- HTML body: `HtmlEncoder.Default` (XSS-safe). Subject/plain text: unencoded
- Subject: static text or config-sourced variables only - never raw user input
- `{{ app_name }}` is automatically available from `EmailOptions.FromName`
