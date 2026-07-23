# /browser-use — AI Browser Automation

Run browser-use to automate web UI tasks with natural language.

## Usage

```
/browser-use <task description or URL>
```

## Examples

```
/browser-use Go to github.com/trending and extract top 10 repos
/browser-use Fill the contact form at example.com/contact with test data
/browser-use Take a screenshot of http://localhost:5110 Keycloak admin
/browser-use Open Keycloak admin at http://localhost:5110 and list realms
```

## Workflow Steps

### Step 1: Activate Skill
// turbo
Read the browser-use skill for reference:
```
view_file /Users/user/Documents/bd-workspace/.agents/skills/browser-use/SKILL.md
```

### Step 2: Check Environment
// turbo
Verify browser-use is installed:
```bash
uv pip show browser-use 2>/dev/null || echo "NOT_INSTALLED"
```

If NOT_INSTALLED:
```bash
uv pip install browser-use && uvx browser-use install
```

### Step 3: Check LLM Keys
// turbo
Verify at least one LLM key is available:
```bash
[ -n "$GOOGLE_API_KEY" ] && echo "GOOGLE_API_KEY ✓" || echo "GOOGLE_API_KEY ✗"
[ -n "$ANTHROPIC_API_KEY" ] && echo "ANTHROPIC_API_KEY ✓" || echo "ANTHROPIC_API_KEY ✗"
[ -n "$OPENAI_API_KEY" ] && echo "OPENAI_API_KEY ✓" || echo "OPENAI_API_KEY ✗"
[ -n "$BROWSER_USE_API_KEY" ] && echo "BROWSER_USE_API_KEY ✓" || echo "BROWSER_USE_API_KEY ✗"
```

If no key is available, ask user to set one.

### Step 4: Create and Run Script

Based on the user's task, create a Python script in the project's scratch directory:

```
/Users/user/Documents/bd-workspace/.agents/scratch/browser-use-task.py
```

**Script template:**

```python
from browser_use import Agent, Browser, BrowserConfig
import asyncio

async def main():
    browser = Browser(
        config=BrowserConfig(
            headless=True,  # Set False for debugging
        )
    )

    agent = Agent(
        task="<USER_TASK_HERE>",
        llm=ChatGoogle(model='gemini-2.5-flash'),  # Adjust based on available key
        browser=browser,
        max_steps=25,  # Safety limit
    )

    result = await agent.run()
    print("=== RESULT ===")
    print(result)
    await browser.close()

asyncio.run(main())
```

Run it:
```bash
cd /Users/user/Documents/bd-workspace && uv run .agents/scratch/browser-use-task.py
```

### Step 5: Report Results

After execution:
1. Parse output and present results to user
2. If screenshots were taken, show file paths
3. If data was extracted, format it nicely
4. If errors occurred, suggest fixes (headless→headed, different LLM, cloud browser)

## Advanced Options

### Use with Structured Output
Add Pydantic models to the script for typed data extraction.

### Use with Custom Tools
Add `@tools.action` decorated functions for file I/O, API calls, etc.

### Use Cloud Browser
Set `Browser(use_cloud=True)` when stealth/proxy is needed.

### Use Real Chrome Profile
Set `chrome_instance_path` to use saved logins/cookies.

## Decision Matrix

| Scenario | Configuration |
|----------|--------------|
| Quick scrape | `headless=True`, `max_steps=15` |
| Form filling | `headless=False` (debug), `max_steps=30` |
| Auth required | Real Chrome profile or cloud profile |
| Bot detection | `use_cloud=True` |
| Data extraction | Add Pydantic `output_schema` |
| Production | Cloud SDK (`browser_use_sdk.v3`) |
