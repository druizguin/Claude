
# Claude desktop
Claude desktop: Modificar manualmente servidores mcp:

Abrir en el perfil ajusteS/desarrollador: editar configuración
Modificar: claude_desktop_config.json

ruta: %AppData%\Claude
```json
{
  "mcpServers": {
    "filesystem": {
      "command": "npx",
      "args": [
        "-y",
        "@modelcontextprotocol/server-filesystem",
        "C:\\Users\\david.ruizguin\\Desktop\\Projects",
        "C:\\Users\\david.ruizguin\\Downloads"
      ]
    },
    "memory": {
      "command": "npx",
      "args": [
        "-y",
        "@modelcontextprotocol/server-memory"
      ],
      "env": {
        "MEMORY_DB_PATH": "C:\\Users\\david.ruizguin\\Desktop\\Projects\\memory.sqlite"
      }
    }
  },
  "__Excluded-mcpServers": {
    "weather-mcp": {
      "command": "py",
      "args": [
        "-3.13",
        "C:\\repos\\3p\\py\\Weather-MCP-ClaudeDesktop\\main.py"
      ],
      "env": {
         "OPENWEATHER_API_KEY": "[API KEY]"
      }
    }
  }
}
``` 


ruta: %AppData%\Claude\logs

Se encuentran todos los registros de comunicación



Wheather:

https://home.openweathermap.org/api_keys

