# TelecomOps.Core — Domain Layer

## Purpose

Contains domain entities, enums, and interfaces. **No external NuGet dependencies** — only primitive types and abstractions.

## Key Components

### Entities

- **NodeConfig**: Represents a 5G network node.
  - `Id` (Guid): Unique identifier
  - `Name` (string): Friendly name
  - `FrequencyBand` (enum): Radio frequency band (N78, N257, etc.)
  - `Status` (enum): Operational state (Active, Inactive, Maintenance, Faulty)
  - `CreatedAt` (DateTime): Timestamp of creation
  - `UpdatedAt` (DateTime?): Last modification time

- **TelemetryEvent**: Metrics data point for a node.
  - `NodeId` (Guid): Reference to NodeConfig
  - `Timestamp` (DateTime): When the event occurred
  - `LatencyMs` (double): Network latency in milliseconds
  - `ThroughputMbps` (double): Data throughput in megabits/sec
  - `Status` (NodeStatus): Node status at time of measurement
  - `AdditionalData` (string?): Optional extra metadata

### Enums

- **FrequencyBand**: Real 5G bands
  - `Band3` = 1800 MHz
  - `Band7` = 2600 MHz
  - `Band28` = 700 MHz
  - `Band78` = 3500 MHz

- **NodeStatus**: Operational states
  - `Active`: Node is operational
  - `Inactive`: Node is offline
  - `Maintenance`: Planned maintenance
  - `Faulty`: Error state

### Interfaces

- **INodeConfigRepository**: CRUD operations for NodeConfig
  - `GetByIdAsync(id)`: Retrieve one node
  - `GetAllAsync()`: Retrieve all nodes
  - `GetActiveNodesAsync()`: Retrieve active nodes only
  - `AddAsync(node)`: Create new node
  - `UpdateAsync(node)`: Update existing node
  - `DeleteAsync(id)`: Delete node

- **IMetricsPublisher**: Publish telemetry data
  - `PublishAsync(event)`: Send metrics to backend (InfluxDB)

### Domain Services

- **NodeConfigService**: Business logic for node configuration
  - `CreateNodeConfigAsync(name, band)`: Validate and create node
  - `UpdateNodeStatusAsync(id, status)`: Change node status

## Dependency Rules

✅ Can use:
- Other Core components
- .NET base classes (string, DateTime, etc.)

❌ Cannot use:
- Infrastructure classes (repositories, DbContext)
- External NuGet packages
- Api or Worker code

## Why this layer exists

- **Separation of concerns**: Business logic independent of implementation details.
- **Testability**: Easy to unit test without mocking databases.
- **Reusability**: Core can be used by API, Worker, CLI, etc.
