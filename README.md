# Kitchen Chaos VR

![Top language](https://img.shields.io/github/languages/top/jke48222/VR-Final-Project) ![engine](https://img.shields.io/badge/engine-Unity-black) ![target](https://img.shields.io/badge/target-Meta%20Quest%203-blue) ![players](https://img.shields.io/badge/players-2-blue)

A two player competitive cooking game for VR headsets. You get 120 seconds, a randomly chosen theme,
and a kitchen full of food you can actually pick up. Chop it, cook it, plate it, and then a judge
character reads out a verdict on both dishes and picks a winner.

Built by two students for a University of Georgia virtual reality course. Runs on Meta Quest
headsets. This repository is source only. There is no packaged build, gameplay capture, or trailer
committed here.

## Team

| | |
| --- | --- |
| <img src="headshotjalen.jpg" alt="Jalen Edusei" width="110"/> | **Jalen Edusei**, Computer Systems Engineering, University of Georgia. jalen.edusei@uga.edu, [github.com/jke48222](https://github.com/jke48222) |
| <img src="headshotjean.jpg" alt="Jean-Guy Leconte" width="110"/> | **Jean-Guy Leconte**, Computer Science, University of Georgia. jel70795@uga.edu, [github.com/lilwooce](https://github.com/lilwooce) |

**Per file authorship is not recoverable from this history, and this README does not guess at it.**
The repository is a re-upload of a GitHub Classroom repository named `final-project-code-uga-vr-final`
onto a personal account. All 9 commits here carry Jalen's name, but commit `dc0e1e1` ("Initial
commit") is a single squashed import of 1,862 files and 883,372 insertions. That import flattened
the original commit history, so nothing in this repository can attribute a file to one of us. Read
every system described below as joint work.

## What problem this solves

Cooking party games score you with a checklist. You either assembled the burger or you did not, and
the game prints a number. That works fine on a screen, but it throws away the thing VR is actually
good at, which is that you physically picked up the tomato, missed the cutting board, and dropped it
on the floor.

We wanted the end of a round to feel like a cooking show verdict rather than a scoreboard: a judge
who talks about your specific plate, names what you got wrong, and announces a winner with some
personality.

The obvious way to build that is to hand the plate contents to a language model and let it decide.
That is also the wrong way. A model that picks the winner makes the game unfair in ways a player
cannot see or appeal, and it makes the game unplayable the moment the network is down. So the
project splits the two jobs: a deterministic C# engine computes the score, and the model is supposed
to be a presentation layer on top of it.

That split is the most interesting idea in this codebase. It is also only half wired, and the
[Status](#status) section says exactly where it stops. We would rather you read that from us than
find it in the source.

## How it works

The Unity project lives in `FinalProject/`, so every `Assets/...` path below is relative to that
directory, which is also what Unity itself shows you.

Four scenes are in the build, and they run in order.

```
Bootstrap.unity          MainMenu.unity        IntroScene.unity        CookingShowScene.unity
-----------------        --------------        ----------------        ----------------------
VelNetBootstrap      ->  pick avatar skin  ->  join room             ->  120 second round
connects and logs in     pick single or        "KitchenChaos",          chaos events, cooking,
to vn.ugavel.com:5002    multiplayer           wait for 2 players       plating, then judging
persists across scenes
```

VelNet is the networking library provided by the UGA VEL lab (Virtual Experiences Laboratory),
installed from a scoped npm registry at `npm.ugavel.com`. `vn.ugavel.com:5002` is the course
provided server.

### Inside a round

`RoundManager.cs` (475 lines) drives the whole 120 seconds. On start it picks one of 29 theme
strings configured in the scene ("Midnight Fridge Raid", "Pantry Roulette", "Oops All Carbs"),
displays it, and speaks it aloud. Then it starts the chaos loop and counts down. When the timer
hits zero it hands both plates to the judge.

The interaction layer underneath is **built on Meta's Oculus Interaction SDK**, not hand authored.
Every grabbable food object carries `Oculus.Interaction.Grabbable`, `GrabInteractable`, and
`GrabFreeTransformer`, 234 instances of each across the prefabs and scenes. What is hand written is
everything the SDK does not do:

| File | Lines | What it does |
| --- | --- | --- |
| `HandController.cs` | 491 | All locomotion, written directly against Unity's Input System rather than a prebuilt locomotion provider. Teleport aimed with a real projectile arc (a stepped simulation with raycasts, 50 arc segments), thumbstick snap turn, GoGo extended reach (an arm scaling technique that lets you grab past your physical arm length), and air grab locomotion (pull the world toward you). |
| `CuttableItem.cs` | 191 | Splits a food object into two halves, but only when it is resting on a cutting board. |
| `CookableItem.cs` | 303 | Cook progress and burn progress over a four state enum (Raw, Cooking, Done, Burnt), driving material color, emission, and sizzle audio. |
| `BowlRecipeCombiner.cs` | 320 | Tracks what is inside a bowl and combines contents into a dish object. |
| `Plate.cs` | 225 | Trigger based tracking of what is sitting on a player's plate. Exposes `GetIngredientIdSet()`, the input to scoring. |

Chaos events are ScriptableObjects (Unity data assets that live as files rather than inside a
scene), so adding one does not require touching `ChaosManager.cs`. Four are implemented:
`LevitatingItemsEvent` (kills gravity and applies upward force), `RandomScaleChaosEvent`,
`RubberKnifeEvent`, and `FlyingPantryFoodEvent`. Every 8 to 15 seconds the manager picks an
off cooldown event, announces it aloud, and runs it. See [Status](#status) for which of the four
can actually fire in the shipped scene.

### The judging split

This is the part worth reading. Two files, two very different jobs.

```
                Plate contents: HashSet<string> of ingredient IDs
                                    |
                                    v
                    DishScorer.ScorePlate()
                    pure C#, no network, no randomness
                                    |
                    +---------------+----------------+
                    |                                |
              score 0 to 100                   breakdown text
                    |                        ("+ Required ingredient
                    |                          present: ing_bread (+20.0)")
                    |                                |
                    |                                v
                    |              AIDishJudgeController.cs + system prompt
                    |                                |
                    |                                v
                    |              OpenAIJudgeClient  ->  api.openai.com
                    |                                |
                    |                                v
                    |              JudgeResponse: per player commentary,
                    |              per player score, winnerIndex, winnerLine
                    |                                |
                    v                                v
        FallbackJudgingSequence          RunJudgingSequence  ->  judge NPC text
        (runs only if the call fails)    (the normal path)
```

**`DishScorer.cs` is fully deterministic.** It takes the set of ingredient IDs on a plate, picks the
best matching recipe from `plate.possibleRecipes` (the recipe whose required ingredients are all
present, breaking ties by required ingredient count), then computes:

- `baseScore` from the recipe, 50 for the burger.
- Plus `weight * requiredIngredientWeight` for each required ingredient present.
- Minus `weight * extraIngredientPenalty` for each required ingredient missing.
- Plus `weight * extraIngredientBonusWeight` for each recipe defined extra present.
- Minus a flat penalty for every ingredient on the plate that the recipe does not name.
- Clamped to 0 through 100.

If no recipe matches at all it awards "pity points", `ingredientCount * 5` capped at 40, for having
plated something. Alongside the number it builds a human readable breakdown string, one line per
scoring decision. No network call, no model, no randomness.

**`AIJudgeClient.cs` is the network layer.** Class `OpenAIJudgeClient`, a raw `UnityWebRequest`
POST to `https://api.openai.com/v1/chat/completions`, no SDK. Model `gpt-4.1`, temperature `0.2`,
`response_format` set to `json_object`.

**`AIDishJudgeController.cs` is the presentation layer.** Its system prompt is a hardcoded `const`
at [lines 42 to 58](FinalProject/Assets/Scripts/AIDishJudgeController.cs#L42-L58). It sends
`JudgeRequestPayload` with a `players[]` array of `{playerIndex, hasDish, localScore, breakdown,
recipeName}`, where `localScore` and `breakdown` come straight from `DishScorer`. It asks for 2 to 3
sentences of TV judge commentary per player, a 0 to 100 score, and a winner pick, and parses the
reply into `JudgeResponse` with `players[{playerIndex, score, commentary[]}]`, `winnerIndex`, and
`winnerLine`.

If the request fails or comes back unusable, `FallbackJudgingSequence` scores both plates locally
with `DishScorer` and announces the higher one. That fallback is genuinely good defensive design:
the round always resolves, with or without a network.

The gap between the intent and the wiring is documented under [Status](#status).

## What is verified

Only things this repository can actually prove.

| Claim | Evidence |
| --- | --- |
| It ran on a real Meta Quest headset | An on device Android logcat (system log) recovered from git history. Deleted in commit `12d4a2b`, still readable at `dc0e1e1` as `KitchenChaos_Debug.txt`, 110,499 lines. Reports `Device Model 'Oculus Quest', OS 'Android OS 14 (API 34)'`, `Unity v6000.2.7f2`, `OVRPlugin v1.113.0`, plus `HMDMounted` and `TrackingAcquired` events. Session runs 12-08 04:22:25 to 04:24:55. |
| It was really built for Android arm64 | A `FinalProject_BurstDebugInformation_DoNotShip/tempburstlibs/arm64-v8a` artifact, present at `dc0e1e1` and deleted in `2d33eba`. Burst is Unity's ahead of time compiler, and that directory is only produced by an actual arm64 Android build. |
| A full gameplay loop worked on device | The same log shows the intro video, VelNet login, room join, round start, ingredient grabbing, cutting (`'cucumber' cut into 'cucumber-half-a(Clone)' and 'cucumber-half-b(Clone)'`), bowl combining, and round end. |
| Networking connects, authenticates, and joins rooms against a live server | Same log: `Logged in. Joining room 'ChaoticKitchen'...` then `Joined room 'ChaoticKitchen'. Current players: 1`. |
| First party code size | 51 first party scripts, 8,126 lines, under `FinalProject/Assets/Scripts/`. Counted with `wc -l`, excluding vendored code. |
| Content counts | 8 recipe ScriptableObjects (`Assets/ScriptableObjects/R_*.asset`) with 5 to 10 ingredient entries each, referencing 47 unique ingredient IDs. 133 of the 158 prefabs in `Assets/Prefabs/` carry an `IngredientDescriptor`, though that count is inflated by duplicates such as `egg (1)` through `egg (6)`. |

**The headset does not identify itself precisely.** The log reports the generic string
`'Oculus Quest'`, not a model number, and the OpenXR settings enable Quest, Quest 2, Quest Pro,
Quest 3, and Quest 3S alike. So the correct claim is "a Meta Quest", not any specific model.

**The captured session predates the current code.** That log calls `RoundManager.EndRound()` and
`RoundManager.JudgePlates()`, neither of which exists in the current source, and logs a `DishScorer`
message ("Plate has no current dish") that is not in the current file either. It also reports
`Required players reached (1/1)`, while the scene in this tree is configured for 2. So it proves the
project ran on a headset and that the interaction systems worked there. It does **not** prove the
current judging code has ever run on device.

**Two clients in one session is not evidenced anywhere.** The networking code is real and
substantial: `VelNetBootstrap.cs` handles persistent connect, login, and scene load;
`NetworkAvatarState.cs` (350 lines) replicates head and hand transforms, grab flags, and skin index
with smoothing; `PlayerSpawnManager.cs` spawns networked players. But the only captured session was
solo, and it ends with a VelNet `IOException` / `SocketException` at teardown. Nothing in this
repository shows two headsets in one room.

**Nothing about performance is measured.** There is no FPS counter, no Unity Profiler capture, no
benchmark, and no frame timing instrumentation anywhere in the tree. This README therefore makes no
performance claim at all. Frame rate matters enormously in VR, and we simply do not have a number.

**There are no tests.** No test assembly definitions, no test files. `ConnectionTester.cs` and
`ChaosTester.cs` are manual in editor debug helpers, not automated tests.

## Running it

### Prerequisites

- **Unity 6000.2.7f2** (`FinalProject/ProjectSettings/ProjectVersion.txt`). Opening on any other
  version triggers Unity's project upgrade, which nobody here has tested. Use this one.
- The **Android Build Support** module for that Unity version, including the OpenJDK and Android SDK
  and NDK sub modules.
- A **Meta Quest** headset in developer mode, plus a USB cable or Meta Quest Link.
- Optional, only for the judge and the spoken lines: an **OpenAI API key**. See below.

Packages resolve automatically from `FinalProject/Packages/manifest.json`, including Meta XR SDK
81.0.0 (`com.meta.xr.sdk.all`), OpenXR 1.15.1, URP 17.2.0 (Universal Render Pipeline, Unity's
scriptable renderer tuned for mobile GPUs), Input System 1.14.2, and VelNet 1.5.7 from the
`npm.ugavel.com` scoped registry. **Note that the VelNet package and the multiplayer server are both
UGA hosted.** If those hosts are unreachable, the project will not resolve packages and multiplayer
will not connect.

One correction worth stating plainly, since the wrong version was published first: **Unity's XR
Interaction Toolkit is not used and does not appear in the manifest.** An earlier revision of this
README described the interaction as "XR Interaction Toolkit-style", which was simply false. Grabbing
and throwing come from the Meta XR SDK, and every bit of locomotion is hand written in
`HandController.cs`.

### Open the project

```bash
git clone <this repo>
```

In Unity Hub, click Add, and select the **`FinalProject/`** directory, not the repository root. The
first import takes a while because `Library/` is gitignored and Unity has to reimport every asset.

Open `Assets/Scenes/Bootstrap.unity` and press Play to run in the editor. Bootstrap connects to
VelNet first and only then loads `MainMenu`, so if the server is unreachable the editor will sit at
the bootstrap scene.

### Supply your own API key

No key is committed. Both key fields are empty in this tree, and you have to fill them in yourself.
There are two separate ones:

1. **The judge.** Open `Assets/Scenes/CookingShowScene.unity`, select the `AIDishJudgeController`
   GameObject, and paste your key into the `Api Key` field on its **OpenAIJudgeClient** component.
   Without it, `SendJudgeRequest` logs an error, returns null, and the round resolves through
   `FallbackJudgingSequence` instead.
2. **The spoken lines.** The theme announcement and the chaos event announcements go through
   OpenAI's text to speech endpoint. Set the `Open AI Key` field on the **OpenAIWrapper** component,
   found on the `OpenAI` prefab (`Assets/Prefabs/Core/OpenAI.prefab`) and the `TTS Setup` prefab.
   Without it, the game runs silently on those lines.

Both fields are plain `[SerializeField]` strings serialized into the scene or prefab, so **anything
you type there gets written into a YAML asset that git will happily commit.** Do not commit your
key. See the security note under [Status](#status) for why that warning is here.

### Build to a headset

1. `File > Build Profiles`, switch the platform to **Android**.
2. Confirm the scene list is Bootstrap, MainMenu, IntroScene, CookingShowScene, in that order.
   Bootstrap must be index 0.
3. Confirm `Edit > Project Settings > XR Plug-in Management > Android` has **OpenXR** enabled with
   the Meta Quest feature group on. Min and target SDK are both 32.
4. Connect the headset, accept the USB debugging prompt inside the headset, and use
   **Build And Run**.

A successful run shows the intro video, then the main menu, then a room join. In multiplayer the
game waits for 2 players in room `KitchenChaos` before loading `CookingShowScene`. Once the round
starts you should see a theme at the top of the canvas and a 120 second countdown.

## Project layout

```
FinalProject/                       Open THIS directory in Unity, not the repo root
├── Assets/
│   ├── Scripts/                    51 first party scripts, 8,126 lines
│   │   ├── RoundManager.cs             Round timer, theme, judging handoff (475)
│   │   ├── HandController.cs           All VR locomotion, hand written (491)
│   │   ├── AIDishJudgeController.cs    Prompt, request, response, playback (503)
│   │   ├── DishScorer.cs               Deterministic scoring engine (228)
│   │   ├── AIJudgeClient.cs            OpenAIJudgeClient, raw UnityWebRequest (176)
│   │   ├── NetworkAvatarState.cs       Avatar pose replication over VelNet (350)
│   │   ├── Editor/                     Custom editor tooling, see below
│   │   ├── Core/, Enums/,              Vendored OpenAI TTS package, third party,
│   │   └── Example Helpers/            7 files, 246 lines, NOT counted above
│   ├── Scenes/                     Bootstrap, MainMenu, IntroScene, CookingShowScene
│   │                               (plus CookingShow.unity, an older copy not in the build)
│   ├── ScriptableObjects/          8 recipes, R_Burger.asset and friends
│   ├── Prefabs/Food/               133 ingredient prefabs
│   ├── Prefabs/Player/             ChefSkin0 to ChefSkin4, the avatar skins
│   ├── ithappy/                    Third party "Creative Characters FREE" pack, 59 C# files
│   │                               Supplies the meshes behind the ChefSkin prefabs
│   ├── Video/                      KitchenChaosIntro.mp4, the intro clip
│   └── *.asset                     The four chaos event ScriptableObjects
├── Packages/manifest.json          Meta XR 81.0.0, OpenXR, URP, VelNet
├── ProjectSettings/                Android target, SDK 32, product name KitchenChaos
└── README.md                       The original course submission writeup, kept as submitted
```

Repo wide there are 117 C# files. Only 51 of them are ours. The other 66 are the vendored OpenAI
text to speech package (7 files, 246 lines) and the `ithappy` character art pack (59 files).

`Assets/Scripts/Editor/VelNetAutoSetupManager.cs` (306 lines) deserves a mention. It is a custom
Unity editor extension that walks a scene or a prefab hierarchy, assigns stable network IDs, and
auto wires the VelNet components onto every object, exposed under a `VelNet/Setup/...` menu. Editor
tooling that removes a repetitive manual step is unusual to find in a one semester course project.

## Status

Playable and demonstrated on hardware. 9 commits, all squashed from a Classroom repository as
described above. The gaps below are real, and they are listed because a reader would otherwise find
them in the source.

### The deterministic guardrail is designed but not enforced

This is the honest version of the headline feature. `DishScorer` is complete and correct, but in the
shipped wiring it does not decide anything the player sees:

- `RunJudgingSequence` displays `pj.score`, which comes from the **model's** `JudgeResponse`, not
  from `DishScorer` (`AIDishJudgeController.cs`, `BuildCommentaryBlock`).
- The winner announcement uses the **model's** `winnerIndex` and `winnerLine`.
- `DishScorer`'s number reaches the model only as `localScore` inside the prompt, and is
  authoritative only inside `FallbackJudgingSequence`, which runs when the API call fails outright.

So the architecture for a model that cannot change the outcome is all there, and the last step is
missing. The fix is small: compute `DishScorer.ScorePlate` once, display that number, decide the
winner from it, and use the model's reply only for the `commentary[]` strings. Until that happens,
a network hiccup or a differently moody response really can change who won.

### "Validated JSON" is generous

The parse is `JsonUtility.FromJson` plus a null check and an empty array check. There is **no schema
validation**. A well formed JSON object with the wrong shape parses into a `JudgeResponse` with
default fields and is accepted.

### `ApplyZeroScoreFix` fabricates scores

At `AIDishJudgeController.cs:238`, any score that comes back as exactly 0.0 is silently replaced
with `Random.Range(10, 51)`, and three sentences of commentary are appended, the last of which
announces that random number to the player as though it were the score. A player who plates nothing
can be told they earned a 37.

### The system prompt instructs the model to lie

Line 54 of the same file reads, in part: `"If there is no dish, there actually is a dish there"`.
That was added to stop the judge from being harsh about empty plates, but it tells the model to
describe food that does not exist. Combined with `ApplyZeroScoreFix`, an empty plate produces
confident commentary about a dish and a fabricated number.

### Text to speech never runs during judging

`RunJudgingSequence` only calls `SetJudgeText` into a `TextMeshProUGUI`. The TTS driven results
path is `RoundManager.ShowResultsSequence`, which sits in the `else` branch that runs **only when
`aiJudgeController` is null** (`RunJudgingAndResults`, `RoundManager.cs:224` to `238`). In
`CookingShowScene.unity` the judge is assigned, so the AI path always runs and the spoken results
path never does. The two are mutually exclusive at runtime.

The TTS that does play during gameplay is the round theme announcement at the top of the round
(`RoundManager.cs:177`) and each chaos event name (`ChaosManager.cs:64`). The judge's verdict is
text on a canvas, silent.

### Only one of the four chaos events can fire

`ChaosManager` in `CookingShowScene.unity` has four entries in its `chaosEvents` list, but all four
point at the same asset GUID, `LevitateItemsEvent`. `RandomScaleChaosEvent`, `RubberKnifeEvent`, and
`FlyingPantryFoodEvent` are all implemented, each has its own ScriptableObject asset, and none of
them is reachable in the shipped scene. This is a scene wiring mistake, not a code one, and it is a
drag and drop away from being fixed.

### Cook state is tracked but never scored

`CookableItem.cs` maintains a full Raw, Cooking, Done, Burnt progression with visual and audio
feedback, and `Recipe.cs` declares `desiredCookState`, `ignoreCookState`, and `expectedCount` per
ingredient. **No script reads any of those three fields.** `DishScorer` works purely from
`Plate.GetIngredientIdSet()`, which is a set of ingredient ID strings with no state and no counts.
A burnt patty scores exactly the same as a perfectly cooked one, and two patties score the same as
one. The data model is ready for it and the scoring function was never extended.

### Security: rotate the keys if they were ever live

Real OpenAI API keys were committed to this project at some point and later scrubbed with BFG Repo
Cleaner, using a `replace.txt` rule of `regex:sk-[A-Za-z0-9_\-]{10,}==>REDACTED` (that rule file is
visible at commit `dc0e1e1` and deleted in `f8ba66e`).

**The current tree is clean.** `apiKey` is `""` in `CookingShowScene.unity`, the TTS key fields are
empty, and a regex sweep for `sk-` prefixed strings across the whole working tree returns nothing.

However, **a BFG scrub only rewrites the repository it is run against.** The pre scrub history very
likely still exists in the original GitHub Classroom repository, and possibly in forks or local
clones of it. If those keys were ever live, they should be treated as exposed and rotated at
platform.openai.com. This is noted here rather than quietly left out, because the responsible thing
after a leak is to say so.

### Smaller things

- `CookingShow.unity` is an older duplicate of the play scene, not in the build list, and should
  probably be deleted.
- `Assets/` contains leftover `.zip.meta` orphans (`Scripts.zip.meta`, `Scripts (2).zip.meta`,
  `ithappy.zip.meta`) whose archives are gone.
- `NetworkGameManager.cs` defaults `gameSceneName` to `"ChaoticKitchen_Multiplayer"`, a scene that
  does not exist. The scene instance in `IntroScene.unity` overrides it to `CookingShowScene`, so it
  works, but a fresh instance of the component would not.
- The judge NPC is not a bespoke character. `judgeRoot` in `CookingShowScene.unity` points at
  `Prefabs/Player/ChefSkin0.prefab`, the same avatar prefab a player can wear, and its meshes come
  from the third party `ithappy` pack. None of that character art is ours.

## Credits

Kitchen Chaos VR was built by Jalen Edusei and Jean-Guy Leconte for a University of Georgia virtual
reality course.

Third party components: Meta XR SDK 81.0.0, VelNet 1.5.7 (UGA Virtual Experiences Laboratory), the
`ithappy` Creative Characters FREE pack, and a vendored OpenAI text to speech Unity wrapper in
`Assets/Scripts/Core/`.
