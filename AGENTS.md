# Sample Unity 2D game

## Context
You are working on a Unity 6 editor through its MCP tools.

## Guidelines
Do not read project asset file contents, unless .cs files. Use Unity MCP tools to work with them if applicable.

## Coding convention
**Private members**
Prefix m_
```
private bool m_IsTicking;
```

**Public members**
First letter capitalized
```
public TerrainManager Terrain { get; set; }
```

**Static members**
Prefix s_
```
private static GameManager s_Instance;
```

**Event Emitter and callback members**
Prefix On
```
// Event emitter
public UnityAction OnDeckLoaded;

// Callback
public void OnThrow()
```