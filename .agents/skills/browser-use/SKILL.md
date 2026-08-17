---
name: ck:browser-use
description: AI-powered browser automation with browser-use (Python). Use for web scraping, form filling, UI testing, data extraction, web interaction, screenshot capture, authenticated browsing, and any task requiring intelligent browser navigation.
argument-hint: "[task description or URL]"
metadata:
  author: task9
  version: "1.0.0"
---

# browser-use Skill

AI-powered browser automation using the [browser-use](https://github.com/browser-use/browser-use) Python library. The agent understands natural language instructions and autonomously navigates, clicks, fills forms, extracts data, and more.

## Prerequisites

Python >= 3.11 required. Install with `uv`:

```bash
# Install browser-use
uv pip install browser-use

# Install Chromium if not present
uvx browser-use install
```

### Environment Setup

Set at least one LLM provider key:

```bash
# Option 1: Browser Use Cloud (recommended for best results)
export BROWSER_USE_API_KEY=your-key

# Option 2: Google Gemini
export GOOGLE_API_KEY=your-key

# Option 3: Anthropic
export ANTHROPIC_API_KEY=your-key

# Option 4: OpenAI
export OPENAI_API_KEY=your-key
```

## When to Use

| Use browser-use | Use agent-browser (CLI) |
|----------------|------------------------|
| Complex multi-step web workflows | Quick element inspection |
| AI-driven decision making on pages | Simple click/fill sequences |
| Data extraction with structured output | Snapshot-based ref navigation |
| Form filling with context awareness | Parallel session management |
| Web scraping at scale | Cloud browser (Browserbase) |
| Authenticated browsing with profiles | State persistence (JSON) |

## Core Usage Patterns

### Pattern 1: Simple Task (Python Script)

Create a Python script and run with `uv run`:

```python
# script.py
from browser_use import Agent, Browser
import asyncio

# Choose your LLM:
# from browser_use import ChatBrowserUse   # Best results
# from browser_use import ChatGoogle       # Gemini
# from browser_use import ChatAnthropic    # Claude

async def main():
    agent = Agent(
        task="Go to example.com and extract the main heading text",
        llm=ChatGoogle(model='gemini-2.5-flash'),
    )
    result = await agent.run()
    print(result)

asyncio.run(main())
```

```bash
uv run script.py
```

### Pattern 2: Custom Browser Configuration

```python
from browser_use import Agent, Browser, BrowserConfig
import asyncio

async def main():
    browser = Browser(
        config=BrowserConfig(
            headless=False,           # Show browser window
            disable_security=False,
            # chrome_instance_path="/usr/bin/google-chrome",  # Use system Chrome
        )
    )

    agent = Agent(
        task="Navigate to github.com and find the trending repositories",
        llm=ChatGoogle(model='gemini-2.5-flash'),
        browser=browser,
    )
    result = await agent.run()
    await browser.close()

asyncio.run(main())
```

### Pattern 3: Custom Tools

Extend the agent with custom actions:

```python
from browser_use import Agent, Tools
import asyncio

tools = Tools()

@tools.action(description='Save extracted data to a JSON file')
def save_data(data: str, filename: str) -> str:
    import json
    with open(filename, 'w') as f:
        json.dump(json.loads(data), f, indent=2)
    return f"Saved to {filename}"

@tools.action(description='Read a file and return its contents')
def read_file(filepath: str) -> str:
    with open(filepath) as f:
        return f.read()

async def main():
    agent = Agent(
        task="Go to news.ycombinator.com, get the top 5 stories, save them to hn_top5.json",
        llm=ChatGoogle(model='gemini-2.5-flash'),
        tools=tools,
    )
    await agent.run()

asyncio.run(main())
```

### Pattern 4: Real Browser Profile (Authenticated Sessions)

```python
from browser_use import Agent, Browser, BrowserConfig
import asyncio

async def main():
    # Use existing Chrome profile with saved logins
    browser = Browser(
        config=BrowserConfig(
            chrome_instance_path="/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
            # On Linux: "/usr/bin/google-chrome"
        )
    )

    agent = Agent(
        task="Go to gmail.com and check unread emails",
        llm=ChatGoogle(model='gemini-2.5-flash'),
        browser=browser,
    )
    await agent.run()

asyncio.run(main())
```

### Pattern 5: Cloud Browser (Stealth + Proxy)

```python
from browser_use import Agent, Browser
import asyncio

async def main():
    browser = Browser(use_cloud=True)  # Requires BROWSER_USE_API_KEY

    agent = Agent(
        task="Search for 'browser automation' on Google and list top 5 results",
        llm=ChatGoogle(model='gemini-2.5-flash'),
        browser=browser,
    )
    await agent.run()
    await browser.close()

asyncio.run(main())
```

## CLI Quick Commands

browser-use also has a persistent CLI daemon for fast iteration:

```bash
browser-use open https://example.com    # Navigate to URL
browser-use state                       # See clickable elements
browser-use click 5                     # Click element by index
browser-use type "Hello"                # Type text
browser-use screenshot page.png         # Take screenshot
browser-use close                       # Close browser
```

## Cloud SDK (for Production)

For scalable, stealth-enabled automation:

```python
from browser_use_sdk.v3 import AsyncBrowserUse

client = AsyncBrowserUse()  # Uses BROWSER_USE_API_KEY

# Simple task
result = await client.run("List the top 20 posts on Hacker News")
print(result.output)

# With structured output
from pydantic import BaseModel

class Post(BaseModel):
    title: str
    points: int
    url: str

class Posts(BaseModel):
    posts: list[Post]

result = await client.run(
    "List the top 10 posts on Hacker News",
    output_schema=Posts,
)
for post in result.output.posts:
    print(f"{post.title} ({post.points} pts)")
```

## Integration with n8n

Browser Use works as a standard HTTP integration in n8n:

1. Create Header Auth credential: `Authorization: Bearer YOUR_API_KEY`
2. POST to `https://api.browser-use.com/api/v3/sessions` with task JSON body
3. Poll GET `https://api.browser-use.com/api/v3/sessions/{id}` until status is `idle`/`stopped`

## MCP Server

Add browser-use as an MCP server for AI coding agents:

```bash
# Claude Code
claude mcp add -t http -H "x-browser-use-api-key: YOUR_KEY" browser-use https://api.browser-use.com/v3/mcp
```

## Tips

1. **Be specific in tasks** — "Go to amazon.com, search for 'wireless headphones', sort by price low-to-high, get the first 3 results" works better than "find headphones"
2. **Use `headless=False`** for debugging to see what the agent does
3. **Chain tasks** with follow-up sessions for complex workflows
4. **Use structured output** (Pydantic models) for data extraction
5. **Set `max_steps`** on Agent to limit execution time
6. **Use cloud browsers** for production (stealth, proxy, anti-detection)

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Chromium not found | `uvx browser-use install` |
| Permission denied | Run with `--with-deps` on Linux |
| Page not loading | Try `headless=False` to debug visually |
| Bot detection | Use `Browser(use_cloud=True)` for stealth |
| Slow execution | Use `ChatBrowserUse()` model for 3-5x speedup |
| Import errors | Ensure Python >= 3.11, `uv pip install browser-use` |

## References

- [GitHub Repository](https://github.com/browser-use/browser-use)
- [Documentation](https://docs.browser-use.com)
- [LLM Full Docs](https://docs.browser-use.com/llms-full.txt)
- [Cloud Dashboard](https://cloud.browser-use.com)
- [Examples](https://github.com/browser-use/browser-use/tree/main/examples)
