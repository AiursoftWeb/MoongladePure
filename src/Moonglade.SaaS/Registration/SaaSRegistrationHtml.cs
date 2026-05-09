using System.Net;

namespace MoongladePure.SaaS.Registration;

public static class SaaSRegistrationHtml
{
    public static string Form(string error = null) => $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Create your MoongladePure site</title>
  <style>
    body { margin: 0; font-family: system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif; background: #f7f5ef; color: #152033; }
    main { min-height: 100vh; display: grid; place-items: center; padding: 32px; }
    form { width: min(100%, 420px); display: grid; gap: 14px; }
    h1 { margin: 0 0 8px; font-size: 38px; line-height: 1.1; }
    label { display: grid; gap: 6px; font-weight: 650; }
    input { min-height: 42px; padding: 0 12px; border: 1px solid #b8c0cc; border-radius: 6px; font: inherit; }
    button { min-height: 44px; border: 0; border-radius: 6px; background: #152033; color: white; font: inherit; font-weight: 700; cursor: pointer; }
    .error { padding: 10px 12px; border-radius: 6px; background: #ffe9e9; color: #8a1f1f; }
  </style>
</head>
<body>
  <main>
    <form method="post" action="/register">
      <h1>Create your site</h1>
      {{ErrorHtml(error)}}
      <label>Username<input name="username" autocomplete="username" required minlength="3" maxlength="32" pattern="[a-z0-9][a-z0-9-]*[a-z0-9]"></label>
      <label>Password<input name="password" type="password" autocomplete="new-password" required minlength="8" maxlength="32"></label>
      <label>Email<input name="email" type="email" autocomplete="email"></label>
      <label>Display name<input name="displayName" autocomplete="name" maxlength="64"></label>
      <label>Site name<input name="siteName" maxlength="128"></label>
      <button type="submit">Create site</button>
    </form>
  </main>
</body>
</html>
""";

    public static string Success(string host) => $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Site created</title>
  <style>
    body { margin: 0; font-family: system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif; background: #f7f5ef; color: #152033; }
    main { min-height: 100vh; display: grid; place-items: center; padding: 32px; }
    section { width: min(100%, 520px); }
    h1 { margin: 0 0 12px; font-size: 38px; line-height: 1.1; }
    p { font-size: 18px; line-height: 1.5; }
    a { color: #152033; font-weight: 700; }
  </style>
</head>
<body>
  <main>
    <section>
      <h1>Site created</h1>
      <p>Your MoongladePure site is ready at <a href="https://{{WebUtility.HtmlEncode(host)}}/">https://{{WebUtility.HtmlEncode(host)}}/</a>.</p>
    </section>
  </main>
</body>
</html>
""";

    private static string ErrorHtml(string error) =>
        string.IsNullOrWhiteSpace(error) ? string.Empty : $"<p class=\"error\">{WebUtility.HtmlEncode(error)}</p>";
}
