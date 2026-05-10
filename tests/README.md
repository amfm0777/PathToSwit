# TelecomOps Tests

Unit and integration tests for the TelecomOps Platform.

## Test Projects

### TelecomOps.Core.Tests

**Purpose**: Unit tests for domain entities and enums.

- `NodeConfigTests`: Tests for `NodeConfig` entity creation and properties.
- `TelemetryEventTests`: Tests for `TelemetryEvent` entity creation and properties.

**Run**:
```bash
dotnet test tests/TelecomOps.Core.Tests/
```

### TelecomOps.Api.Tests

**Purpose**: Tests for API layer using mocks.

- `NodeConfigRepositoryMockTests`: Mock tests for `INodeConfigRepository` interface.
  - `GetAllAsync` returns all nodes
  - `GetActiveNodesAsync` returns only active nodes
  - `AddAsync` calls repository correctly

- `MetricsPublisherMockTests`: Mock tests for `IMetricsPublisher` interface.
  - `PublishAsync` is called with valid events
  - Handles multiple concurrent events

**Run**:
```bash
dotnet test tests/TelecomOps.Api.Tests/
```

### TelecomOps.Worker.Tests

**Purpose**: Tests for Worker background service using mocks.

- `TelemetryWorkerMockTests`: Mock tests for Worker behavior.
  - Worker queries active nodes from repository
  - Worker publishes metrics for each node
  - Worker handles empty node list
  - Worker scales to multiple nodes (parameterized test)

**Run**:
```bash
dotnet test tests/TelecomOps.Worker.Tests/
```

## Running All Tests

```bash
# Run all test projects
dotnet test

# Run with verbose output
dotnet test --verbosity detailed

# Run with coverage (requires dotnet-coverage tool)
dotnet coverage collect dotnet test
```

## Test Frameworks

- **xUnit**: Test runner (modern, clean syntax)
- **FluentAssertions**: Readable assertions
- **Moq**: Mocking framework for interfaces

## Test Structure

Each test follows the AAA pattern:

```csharp
[Fact]
public async Task When_Condition_Should_ExpectedBehavior()
{
    // Arrange: Setup test data and mocks
    var mock = new Mock<IRepository>();
    var input = new TestData();

    // Act: Execute the method/behavior
    var result = await mock.Object.DoSomethingAsync(input);

    // Assert: Verify results
    result.Should().Be(expectedValue);
    mock.Verify(m => m.DoSomethingAsync(input), Times.Once);
}
```

## Current Test Coverage

| Project | Entity/Interface | Coverage |
|---------|------------------|----------|
| Core | `NodeConfig` | ✓ Creation, properties, all enums |
| Core | `TelemetryEvent` | ✓ Creation, properties, nullable fields |
| Api | `INodeConfigRepository` | ✓ GetAll, GetActive, Add |
| Api | `IMetricsPublisher` | ✓ PublishAsync, multiple events |
| Worker | Worker behavior | ✓ Query nodes, publish events, scale |

## Future Test Additions

- ✅ Integration tests with real `AppDbContext`
- ✅ WebApplicationFactory tests for API endpoints
- ✅ Database migration tests
- ✅ End-to-end tests with Docker Compose
- ✅ Performance/load tests

## Notes

- Tests are **not executed** by default; they're ready to run when needed.
- All mocks use `Moq` for clean interface mocking.
- No database or external services required (mocked).
- Tests run in isolation and don't affect Docker Compose stack.
