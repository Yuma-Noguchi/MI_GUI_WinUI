# Stable Diffusion MCP Server Implementation

## Overview
The Stable Diffusion MCP server provides icon generation capabilities through a locally running model. It handles model loading, inference, and image processing, exposing these capabilities through a well-defined API.

## Server Implementation

### Directory Structure
```
stable-diffusion-mcp/
├── src/
│   ├── index.ts               # Main server entry point
│   ├── model.ts              # Model management
│   ├── inference.ts          # Inference pipeline
│   └── utils/
│       ├── image.ts          # Image processing utilities
│       └── prompt.ts         # Prompt enhancement utilities
├── package.json
└── tsconfig.json
```

### Core Components

1. **Model Management**
```typescript
interface ModelConfig {
    modelPath: string;
    deviceType: 'cpu' | 'cuda';
    precision: 'fp32' | 'fp16';
}

class StableDiffusionModel {
    constructor(config: ModelConfig);
    async load(): Promise<void>;
    async unload(): Promise<void>;
    async generate(prompt: string, settings: GenerationSettings): Promise<Buffer>;
    async imageToImage(image: Buffer, prompt: string, settings: GenerationSettings): Promise<Buffer>;
}
```

2. **MCP Server Configuration**
```typescript
interface ServerConfig {
    port: number;
    modelConfig: ModelConfig;
    outputDir: string;
    maxBatchSize: number;
    timeoutMs: number;
}
```

### MCP Tools and Resources

1. **Tools**
```typescript
// Text to image generation
{
    name: 'generate_icon',
    description: 'Generate an icon from text prompt',
    inputSchema: {
        type: 'object',
        properties: {
            prompt: {
                type: 'string',
                description: 'Text description of the desired icon'
            },
            settings: {
                type: 'object',
                properties: {
                    width: { type: 'number', default: 512 },
                    height: { type: 'number', default: 512 },
                    steps: { type: 'number', default: 50 },
                    guidance_scale: { type: 'number', default: 7.5 }
                }
            }
        },
        required: ['prompt']
    }
}

// Image to image generation
{
    name: 'refine_icon',
    description: 'Refine an existing icon with additional prompts',
    inputSchema: {
        type: 'object',
        properties: {
            image: {
                type: 'string',
                description: 'Base64 encoded image data'
            },
            prompt: {
                type: 'string',
                description: 'Additional text prompts for refinement'
            },
            strength: {
                type: 'number',
                description: 'How much to preserve of the original image',
                default: 0.7
            }
        },
        required: ['image', 'prompt']
    }
}
```

2. **Resources**
```typescript
// Model status resource
{
    uri: 'sd://model/status',
    description: 'Current model status and statistics'
}

// Generation history
{
    uriTemplate: 'sd://history/{date}',
    description: 'Generation history for a specific date'
}
```

### Implementation

1. **Server Entry Point**
```typescript
#!/usr/bin/env node
import { Server } from '@modelcontextprotocol/sdk/server';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio';
import { StableDiffusionModel } from './model';
import { ImageProcessor } from './utils/image';

class StableDiffusionServer {
    private server: Server;
    private model: StableDiffusionModel;
    
    constructor(config: ServerConfig) {
        this.server = new Server({
            name: 'stable-diffusion-server',
            version: '0.1.0'
        });
        
        this.model = new StableDiffusionModel(config.modelConfig);
        this.setupTools();
        this.setupResources();
    }
    
    private setupTools() {
        this.server.setRequestHandler(CallToolRequestSchema, async (request) => {
            switch (request.params.name) {
                case 'generate_icon':
                    return this.handleGeneration(request.params.arguments);
                case 'refine_icon':
                    return this.handleRefinement(request.params.arguments);
                default:
                    throw new McpError(ErrorCode.MethodNotFound);
            }
        });
    }
    
    async start() {
        await this.model.load();
        const transport = new StdioServerTransport();
        await this.server.connect(transport);
    }
}
```

2. **Model Integration**
```typescript
import * as onnx from 'onnxruntime-node';
import { loadModel, preprocess, postprocess } from './utils';

export class StableDiffusionModel {
    private session: onnx.InferenceSession;
    
    async generate(prompt: string, settings: GenerationSettings): Promise<Buffer> {
        const input = await preprocess(prompt);
        const output = await this.session.run(input);
        return postprocess(output);
    }
}
```

### Error Handling

```typescript
class StableDiffusionError extends Error {
    constructor(
        public code: string,
        message: string,
        public details?: any
    ) {
        super(message);
    }
}

const ErrorCodes = {
    MODEL_LOAD_FAILED: 'MODEL_LOAD_FAILED',
    GENERATION_FAILED: 'GENERATION_FAILED',
    INVALID_INPUT: 'INVALID_INPUT',
    TIMEOUT: 'TIMEOUT'
} as const;
```

### Configuration

```json
{
    "mcpServers": {
        "stable-diffusion": {
            "command": "node",
            "args": ["path/to/stable-diffusion-mcp/build/index.js"],
            "env": {
                "MODEL_PATH": "path/to/model.onnx",
                "DEVICE_TYPE": "cuda",
                "OUTPUT_DIR": "path/to/output"
            }
        }
    }
}
```

## Usage from WinUI Application

```csharp
public class IconStudioViewModel
{
    private readonly IMcpService _mcpService;
    
    public async Task GenerateIcon(string prompt)
    {
        var result = await _mcpService.CallTool(
            "stable-diffusion",
            "generate_icon",
            new { prompt, settings = new { width = 512, height = 512 } }
        );
        
        // Process result
    }
}
```

## Setup Instructions

1. Install Dependencies
```bash
npm install @modelcontextprotocol/sdk onnxruntime-node sharp
```

2. Build Server
```bash
npm run build
```

3. Configure MCP Settings
- Add server configuration to MCP settings
- Set environment variables for model path and device type
- Configure output directory

4. Test Integration
```csharp
// Verify server connection
var status = await _mcpService.GetResource("sd://model/status");
Console.WriteLine($"Model Status: {status}");
```

This MCP server implementation provides a robust foundation for integrating Stable Diffusion into the Icon Studio feature, with proper error handling, resource management, and a clean API for the WinUI application to consume.