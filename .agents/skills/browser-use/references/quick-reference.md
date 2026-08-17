# browser-use Quick Reference

## Open-Source Python API

### Installation
```bash
uv init && uv add browser-use && uv sync
uvx browser-use install  # Install Chromium
```

### Basic Agent
```python
from browser_use import Agent, Browser, ChatBrowserUse
# from browser_use import ChatGoogle       # ChatGoogle(model='gemini-2.5-flash')
# from browser_use import ChatAnthropic    # ChatAnthropic(model='claude-sonnet-4-6')
import asyncio

async def main():
    agent = Agent(
        task="Your task here",
        llm=ChatBrowserUse(),  # Or ChatGoogle, ChatAnthropic
        browser=Browser(),
    )
    await agent.run()

asyncio.run(main())
```

### Browser Configuration
```python
from browser_use import Browser, BrowserConfig

browser = Browser(
    config=BrowserConfig(
        headless=False,              # Show window
        disable_security=False,
        # chrome_instance_path=...,  # Use system Chrome
    )
)
# Or cloud browser:
browser = Browser(use_cloud=True)    # Requires BROWSER_USE_API_KEY
```

### Custom Tools
```python
from browser_use import Tools

tools = Tools()

@tools.action(description='Description of what this tool does.')
def custom_tool(param: str) -> str:
    return f"Result: {param}"

agent = Agent(task="...", llm=llm, browser=browser, tools=tools)
```

### Authentication (Real Chrome Profile)
```python
browser = Browser(
    config=BrowserConfig(
        chrome_instance_path="/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
    )
)
```

## CLI Commands
```bash
browser-use open <url>           # Navigate
browser-use state                # Get clickable elements
browser-use click <index>        # Click element
browser-use type "text"          # Type text
browser-use input <index> "text" # Clear + type into element
browser-use keys "Enter"         # Send key
browser-use screenshot [path]    # Screenshot
browser-use close                # Close browser
browser-use connect              # Connect to user's Chrome
browser-use cloud connect        # Cloud browser
browser-use doctor               # Diagnostics
```

## Cloud SDK (Production)

### Install
```bash
pip install browser-use-sdk
```

### Simple Task
```python
from browser_use_sdk.v3 import AsyncBrowserUse

client = AsyncBrowserUse()
result = await client.run("Your task here")
print(result.output)
```

### Structured Output
```python
from pydantic import BaseModel

class MySchema(BaseModel):
    items: list[str]
    count: int

result = await client.run("Extract...", output_schema=MySchema)
print(result.output.items)
```

### Follow-up Tasks (Session Reuse)
```python
session = await client.sessions.create()
result1 = await client.run("First task", session_id=session.id)
result2 = await client.run("Follow-up", session_id=session.id)
await client.sessions.stop(session.id)
```

### Streaming Messages
```python
run = client.run("Your task")
async for msg in run:
    print(f"[{msg.role}] {msg.summary}")
print(run.result.output)
```

### Proxy & Stealth
```python
result = await client.run("...", proxy_country_code="de")
browser = await client.browsers.create(proxy_country_code="jp")
```

### Browser Profiles (Persistent Auth)
```python
profile = await client.profiles.create(name="my-account")
session = await client.sessions.create(profile_id=profile.id)
result = await client.run("...", session_id=session.id)
await client.sessions.stop(session.id)  # Saves profile state
```

### Recording
```python
result = await client.run("...", enable_recording=True)
urls = await client.sessions.wait_for_recording(result.id)
for url in urls:
    print(url)  # MP4 download URL
```

### Workspaces (File Upload/Download)
```python
workspace = await client.workspaces.create(name="my-workspace")
await client.workspaces.upload(workspace.id, "data.csv")
result = await client.run("Read data.csv", workspace_id=workspace.id)
paths = await client.workspaces.download_all(workspace.id, to="./output")
```

### Deterministic Rerun (Cached Scripts)
```python
# First call: agent runs, caches script
result = await client.run(
    "Get top @{{5}} stories from https://news.ycombinator.com",
    workspace_id=str(workspace.id),
)
# Second call: cached, $0 LLM cost
result = await client.run(
    "Get top @{{10}} stories from https://news.ycombinator.com",
    workspace_id=str(workspace.id),
)
```

## MCP Server
```bash
# Claude Code
claude mcp add -t http -H "x-browser-use-api-key: KEY" browser-use https://api.browser-use.com/v3/mcp

# Or in claude_desktop_config.json / .cursor/mcp.json:
{
  "mcpServers": {
    "browser-use": {
      "url": "https://api.browser-use.com/v3/mcp",
      "headers": { "x-browser-use-api-key": "YOUR_KEY" }
    }
  }
}
```

## n8n Integration
1. Header Auth: `Authorization: Bearer YOUR_API_KEY`
2. POST `https://api.browser-use.com/api/v3/sessions` with `{"task": "..."}`
3. Poll GET `.../sessions/{id}` until `status` is `idle`/`stopped`

## Supported LLM Providers
| Provider | Import | Model Example |
|----------|--------|---------------|
| Browser Use | `ChatBrowserUse()` | Default (best for browser tasks) |
| Google | `ChatGoogle(model='gemini-2.5-flash')` | gemini-2.5-flash |
| Anthropic | `ChatAnthropic(model='claude-sonnet-4-6')` | claude-sonnet-4-6 |
| OpenAI | Via langchain-openai | gpt-4o |
| Ollama | Via langchain-ollama | Local models |
