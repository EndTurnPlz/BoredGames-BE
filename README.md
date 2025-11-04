# BoredGames

A real-time multiplayer board game platform built with ASP.NET Core, featuring WebSocket-based gameplay and extensible game architecture.

> **Note:** The public server is currently offline due to cloud computing costs. Follow the instructions below to run the application locally.

## Overview

BoredGames is a full-stack multiplayer gaming platform that brings classic board games to the web. The backend provides a robust, real-time game server capable of managing multiple concurrent game rooms, player connections, and game state synchronization. The platform currently features two fully-implemented games: **Apologies** (a Sorry!-inspired game) and **Ups and Downs** (a Snakes and Ladders variant).

The system uses Server-Sent Events (SSE) for efficient real-time communication, delivering player-specific game snapshots that update instantly as game state changes. The architecture is designed to scale horizontally and handle hundreds of concurrent players across multiple game sessions.

**Frontend Repository:** [BoredGames-FR](https://github.com/EndTurnPlz/BoredGames-FR)

---

## Technical Architecture & Design Decisions

### Clean Architecture with Domain-Driven Design

The codebase follows a layered architecture pattern that separates concerns and promotes maintainability:

- **`BoredGames.Core`**: Contains all game logic abstractions, room management, and core domain models. This layer has zero dependencies on infrastructure concerns.
- **`BoredGames.Api`**: Thin API layer focused solely on HTTP/SSE communication and routing. Controllers delegate business logic to services.
- **`BoredGames.Games.*`**: Individual game implementations that extend the core framework. Each game is a self-contained module.

This separation enables:
- **Independent testing** of business logic without HTTP concerns
- **Easy addition of new games** without modifying core infrastructure
- **Clear boundaries** between domain logic and presentation

### Event-Driven Game State Management

The system employs an event-driven architecture for propagating game state changes:

```csharp
// GameRoom emits events when state changes
public event EventHandler? RoomChanged;

// RoomManager subscribes and pushes updates to connected players
private async void OnRoomChanged(object? sender, EventArgs e)
{
    await playerConnectionManager.PushSnapshotsToPlayersAsync(...);
}
```

**Benefits:**
- **Real-time responsiveness**: State changes propagate immediately to all players
- **Decoupling**: Game logic doesn't need to know about connection management
- **Player-specific snapshots**: Each player receives customized views of the game state

### Concurrent Room Management

The platform uses `ConcurrentDictionary` for thread-safe room management and fine-grained locking within individual game rooms:

```csharp
private readonly ConcurrentDictionary<Guid, GameRoom> _rooms = new();
private readonly Lock _lock = new(); // Per-room lock
```

**Concurrency Strategy:**
- **Coarse-grained synchronization** at the collection level for adding/removing rooms
- **Fine-grained locking** within individual rooms to avoid blocking unrelated game sessions
- **Lock validation** ensures thread safety (e.g., `EmitRoomChangedEvent` validates lock ownership)

This approach maximizes throughput while preventing race conditions.

### Attribute-Based Game Registration

Games self-register using attributes, eliminating manual configuration:

```csharp
[BoredGame("Apologies")]
[GamePlayerCount(numPlayers: 4)]
public sealed class ApologiesGame : GameBase { ... }
```

The `GameRegistry` uses reflection to discover and register all games at startup. This pattern:
- **Reduces boilerplate**: No manual registration code required
- **Prevents errors**: Compile-time validation of game metadata
- **Supports hot-swapping**: New games can be added by dropping in DLLs

### Resource Lifecycle Management

The platform implements sophisticated resource cleanup:

- **Abandoned room detection**: Rooms with no active players are automatically cleaned up after a timeout
- **Idle game timeout**: Long-running inactive games are pruned to free resources
- **Expired player removal**: Players who disconnect during lobby phase are removed
- **Background cleanup service**: `RoomCleanupService` runs periodic maintenance tasks

```csharp
public bool IsDead(TimeSpan abandonedRoomTimeout, TimeSpan idleGameTimeout)
{
    if (_players.Count == 0 && !_pendingPlayers.Contains(_host)) return true;

    var timeout = RoomState is State.WaitingForPlayers
        ? abandonedRoomTimeout
        : idleGameTimeout;
    return DateTime.Now - LastIdleAt > timeout;
}
```

This ensures the server remains healthy under sustained load without memory leaks.

### Server-Sent Events for Real-Time Communication

The platform uses SSE instead of WebSockets for unidirectional server-to-client streaming:

**Advantages:**
- **Automatic reconnection**: Browsers handle reconnection logic
- **HTTP-based**: Works through most corporate firewalls
- **Lower overhead**: No bidirectional handshake required
- **Built-in event framing**: Natural fit for game state updates

The `PlayerConnectionManager` maintains active `HttpResponse` streams and pushes JSON-serialized snapshots directly to clients.

### Type-Safe Action Dispatch

Game actions use attribute-based routing with reflection-based dispatch:

```csharp
[GameAction("move")]
private void MovePawnAction(Player player, ActionArgs.MovePawnArgs req) { ... }
```

The `GameBase` class discovers all `[GameAction]` methods at runtime and dispatches incoming requests to the appropriate handler. This provides:
- **Type safety**: Action arguments are strongly typed and validated
- **Discoverability**: All actions are documented through attributes
- **Separation of concerns**: Each action is an isolated method

### Immutable Data Structures

The game engine leverages `ImmutableList<Player>` to prevent accidental state mutations:

```csharp
public GameRoom(IGameConfig gameConfig, GameConstructor gameConstructor,
    int minPlayers, int maxPlayers, Player host)
{
    _game = _gameConstructor(_gameConfig, _players.ToImmutableList());
}
```

This prevents subtle bugs where game logic inadvertently modifies the player list.

### Polymorphic Serialization with System.Text.Json

The platform uses custom `JsonTypeInfoResolver` to handle polymorphic game snapshots:

```csharp
options.JsonSerializerOptions.TypeInfoResolver = new GameTypeInfoResolver();
```

This enables different games to return different snapshot types while maintaining type safety and avoiding reflection overhead during serialization.

### Docker-Ready Deployment

The included multi-stage Dockerfile optimizes for production deployments:
- **Build isolation**: Dependencies are restored in a separate layer
- **Layer caching**: Minimal rebuilds on code changes
- **Small image size**: Uses `aspnet` runtime image (not SDK)
- **Port configuration**: Standardized on port 8080

### Testing Infrastructure

The solution includes dedicated test projects for each game:
- `BoredGames.UnitTests.Apologies`
- `BoredGames.UnitTests.UpsAndDowns`

Tests are isolated from infrastructure concerns due to the clean architecture.

---

## Key Strengths

### Scalability
- **Horizontal scaling**: Stateless API design allows multiple instances behind a load balancer
- **Efficient resource usage**: Fine-grained locking and automatic cleanup prevent resource exhaustion
- **Minimal memory footprint**: Immutable data structures and event-driven updates reduce memory churn

### Extensibility
- **Pluggable game framework**: Add new games by implementing `GameBase`
- **Declarative configuration**: Games declare their requirements via attributes
- **Reusable components**: Dice, decks, and board abstractions are shared across games

### Reliability
- **Comprehensive error handling**: Custom exception types for each failure mode
- **Graceful degradation**: Player disconnections don't crash games
- **Automatic recovery**: Resource cleanup ensures system health

### Developer Experience
- **Swagger integration**: Auto-generated API documentation for all endpoints
- **Clear separation of concerns**: Easy to reason about code flow
- **Minimal boilerplate**: Attributes and reflection eliminate repetitive code

---

## Running Locally

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- (Optional) Docker for containerized deployment

### Running with .NET CLI

1. **Clone the repository**
   ```bash
   git clone https://github.com/your-username/BoredGames-BE.git
   cd BoredGames-BE
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Run the application**
   ```bash
   dotnet run --project src/BoredGames.Api/BoredGames.Api.csproj
   ```

4. **Access the API**
   - API Base: `http://localhost:5000`
   - Swagger UI: `http://localhost:5000/swagger`

### Running with Docker

1. **Build the Docker image**
   ```bash
   docker build -t boredgames-backend .
   ```

2. **Run the container**
   ```bash
   docker run -p 8080:8080 boredgames-backend
   ```

3. **Access the API**
   - API Base: `http://localhost:8080`

### Running the Full Stack

1. **Start the backend** (follow steps above)

2. **Clone and run the frontend**
   ```bash
   git clone https://github.com/EndTurnPlz/BoredGames-FR.git
   cd BoredGames-FR
   # Follow frontend README instructions
   ```

3. **Configure frontend API endpoint**
   - Set the API base URL to `http://localhost:5000` (or `8080` if using Docker)

---

## API Endpoints

### Room Management
- `POST /api/room/create?gameType={gameType}` - Create a new game room
- `POST /api/room/{roomId}/join` - Join an existing room
- `GET /api/room/{roomId}/stream?playerId={playerId}` - Connect to room via SSE

### Game Actions
- `POST /api/game/{roomId}/start?playerId={playerId}` - Start the game (host only)
- `POST /api/game/{roomId}/action?playerId={playerId}` - Execute game action

---

## Available Games

### Apologies
A 4-player board game where players race pawns around the board using cards.
- **Players:** 4
- **Duration:** 15-30 minutes
- **Strategy:** Card management and pawn positioning

### Ups and Downs
A Snakes and Ladders variant supporting 2-8 players.
- **Players:** 2-8
- **Duration:** 10-20 minutes
- **Mechanics:** Dice rolling and warp tiles

---

## Project Structure

```
BoredGames-BE/
├── src/
│   ├── BoredGames.Api/          # HTTP controllers and Program.cs
│   ├── BoredGames.Core/         # Game framework and room management
│   ├── BoredGames.Games.Apologies/
│   └── BoredGames.Games.UpsAndDowns/
├── test/
│   └── unit/                    # Unit tests for each game
├── Dockerfile                   # Multi-stage build configuration
└── BoredGames.sln              # Solution file
```

---

## Configuration

The application uses standard ASP.NET Core configuration:
- `appsettings.json` - Production settings
- `appsettings.Development.json` - Development overrides

Key configuration points:
- **CORS origins**: Configured in `Program.cs` (`localhost` for dev, `endturnplz.github.io` for prod)
- **Timeouts**: Room cleanup intervals configured in `RoomManager`
- **Ports**: Default 5000 (local), 8080 (Docker)

---

## Contributing

New games can be added by:
1. Creating a new project under `src/BoredGames.Games.{GameName}`
2. Implementing `GameBase` with `[BoredGame]` attribute
3. Adding corresponding unit tests

---

## License

[Your License Here]
