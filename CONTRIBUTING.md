# Contributing to DataQueryExplorer

Thank you for considering a contribution!

## Getting Started

1. **Fork** the repository on GitHub
2. **Clone** your fork locally
   ```bash
   git clone https://github.com/<your-username>/DataQueryExplorer.git
   ```
3. Create a **feature branch**
   ```bash
   git checkout -b feature/my-improvement
   ```
4. Make your changes — see conventions below
5. Run the tests
   ```bash
   dotnet test
   ```
6. **Commit** with a descriptive message
   ```bash
   git commit -m "feat: add CSV storage writer"
   ```
7. **Push** and open a **Pull Request** against `main`

## Code Conventions

- Target **net8.0** only
- All public-facing packages must be **MIT, Apache 2.0 or BSD-2**-licensed (no LGPL/GPL)
- Follow **clean architecture** — no Infrastructure or Console code in Domain or Application
- New strategies must extend `QueryStrategyBase` and be registered in `Program.cs` + `QueryStrategyFactory`
- Write **xUnit tests** for any new business logic
- No hardcoded team/company names; keep everything generic

## Reporting Bugs

Open a GitHub Issue with:
- Steps to reproduce
- Expected vs. actual behaviour
- Stack trace or log snippet (scrub any sensitive endpoint/key data)
