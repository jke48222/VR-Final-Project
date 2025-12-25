# Kitchen Chaos VR

## Objective
Build a multiplayer VR cooking game that combines physics-based interaction, round-based gameplay, chaotic environmental modifiers, and an AI-driven judge to deliver a complete, replayable VR experience inspired by cooperative party games.

## Description
Kitchen Chaos VR is a timed, round-based VR cooking experience where multiple players prepare dishes under pressure using interactive kitchen props and ingredient objects. Each round presents a rotating set of recipes. Players must gather ingredients, prepare items using tools and heat sources, and plate completed dishes for scoring.

The game blends cooperative and competitive elements. Players share the same kitchen space, compete for better dish quality, and adapt in real time when chaos events alter the environment. At the end of each round, each player’s dish is evaluated with a deterministic scoring system that checks ingredient correctness and cooking state. Final judging is then presented as a dramatic reveal, enhanced by an AI judge that provides structured feedback and narration.

The project emphasizes end-to-end VR systems engineering: multiplayer synchronization, stable physics interactions, modular gameplay systems, UI orchestration, event-driven round flow, and safe integration of external AI services.

## How to Run
- **Unity version:** 6000.2.7f2  
- **XR runtime:** OpenXR  
- **Build target:** Android (standalone VR headset workflow)  
- Open scene: `Assets/Scenes/Bootstrap.unity`  
- Ensure required packages are installed via `Packages/manifest.json`.  
- Build and deploy to headset using the Android build pipeline.

### Multiplayer Notes
- Multiplayer uses **VelNet**.  
- The bootstrap scene initializes networking, persistent managers, and transitions into menu and gameplay scenes.

## Controls (typical VR configuration)
- **Thumbsticks**
  - Locomotion and turning, depending on rig configuration in the scene.
- **Grip / Trigger**
  - Grab, manipulate, and release objects using physics-based interaction.
- **UI Interaction**
  - Interact with menus and panels through VR pointer or direct interaction based on the configured XR rig.

## Key Scripts

| Script | Description |
|--------|-------------|
| `RoundManager.cs` | Central orchestration for round start/end, timers, player plate assignment, judging flow, chaos phase coordination, and UI updates. |
| `DishScorer.cs` | Deterministic dish evaluation engine that compares plated contents against recipe requirements and produces a numeric score plus a structured breakdown. |
| `Plate.cs` | Tracks ingredients and interactables on the player’s plate using trigger enter/exit logic for scoring inputs. |
| `CookableItem.cs` | Defines cook state transitions and tracks cooking progress for ingredients affected by heat sources. |
| `HeatSource.cs` | Applies cooking logic to items within range and drives cook state progression during active heating. |
| `Recipe.cs` | ScriptableObject representation of a recipe, including weighted ingredient requirements and scoring influence. |
| `RecipeBookManager.cs` | Manages in-world recipe book content and UI state across recipe rotations and page changes. |
| `ChaosManager.cs` | Controls chaos phase timing and activation, selects chaos events, and ensures clean start and teardown per round. |
| `ChaosEvent.cs` | Abstract base class for all chaos events, defining consistent `StartEvent` and `EndEvent` interfaces. |
| `LevitatingItemsEvent.cs` | Chaos event that removes gravity and applies forces to tagged kitchen objects for temporary environmental disruption. |
| `RandomScaleChaosEvent.cs` | Chaos event that dynamically changes scale of targeted objects to impact handling and spatial reasoning. |
| `RubberKnifeEvent.cs` | Chaos event that modifies knife behavior and physics characteristics during active chaos. |
| `AIDishJudgeController.cs` | Bridges deterministic scoring output into the AI judge flow and coordinates reveal sequencing with narration. |
| `AIJudgeClient.cs` | HTTP client that calls the OpenAI Chat Completions endpoint and parses strict JSON responses for judge output. |
| `TTSManager.cs` | Text-to-speech management for judge narration and voice feedback orchestration. |
| `VelNetBootstrap.cs` | Persistent networking bootstrap to initialize VelNet and maintain session state across scene loads. |
| `PlayerSpawnManager.cs` | Spawns players and avatars into the scene once network identity and room state are ready. |
| `NetworkAvatarState.cs` | Synchronizes remote avatar pose and transform data for multiplayer representation. |

## Implementation Details
- **Scene bootstrapping and persistence:**  
  A dedicated bootstrap scene initializes networking and persistent managers before routing into menu and gameplay scenes. Persistent objects use a `DontDestroyOnLoad` style approach to keep multiplayer state stable across transitions.

- **Round-based orchestration:**  
  `RoundManager` controls the game loop, including timing, player readiness, recipe rotation, chaos phase windows, and multi-step judging reveals. This script serves as the primary integration layer across gameplay, UI, audio, and AI judging.

- **Deterministic scoring with explainability:**  
  Dish evaluation uses deterministic checks rather than AI judgment for fairness. `DishScorer` produces both a numeric score and a human-readable breakdown, enabling transparent player feedback and making downstream AI commentary grounded in concrete game state.

- **Recipe system as data assets:**  
  Recipes are ScriptableObjects, allowing easy iteration on requirements, weighting, and variations without modifying code. This supports scalable content expansion through data-driven design.

- **Chaos system as extensible events:**  
  Chaos events derive from `ChaosEvent` and implement explicit start and end logic. Chaos effects typically operate on targeted objects discovered via tags or assigned lists, allowing new disruptive mechanics to be added without editing the core round logic.

- **Multiplayer architecture:**  
  VelNet manages room connection, player spawning, and networked avatars. The system ensures players join sessions, load into scenes consistently, and replicate avatar motion and interaction state as required for a shared kitchen experience.

- **AI judge integration with guardrails:**  
  The AI judge enhances presentation rather than determining the score. The client requests strict JSON-only output and uses deterministic scoring breakdown text as context. Narration is driven through a TTS layer to create a staged judging reveal.

## Reflection
This project strengthened my ability to build a complete VR game pipeline that integrates multiple complex systems into a cohesive experience. I learned how to structure a round-based gameplay loop that remains stable in multiplayer, how to keep scoring deterministic while still providing rich feedback, and how to design a chaos framework that is modular and extensible. Integrating an AI judge reinforced the importance of guardrails and reliability when connecting external services to gameplay, and the overall build required careful attention to scene lifecycle, physics stability, and system orchestration in a production-style Unity project.
