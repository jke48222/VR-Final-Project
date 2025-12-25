# VR Final Project
## Team Members
<p align="center">
  <img src="headshotjalen.jpg" alt="Jalen Edusei" width="160" style="border-radius:50%;"/>
</p>

**Jalen Edusei**  
*Computer Systems Engineering Student, University of Georgia*  
📧 jalen.edusei@uga.edu  
🔗 [LinkedIn](https://www.linkedin.com/in/jalenedusei) • [GitHub](https://github.com/jke48222)

<p align="center">
  <img src="headshotjean.jpg" alt="Jean-Guy Leconte" width="160" style="border-radius:50%;"/>
</p>

**Jean-Guy Leconte**  
*Computer Science Student, University of Georgia*  
📧 jel70795@uga.edu  
🔗 [LinkedIn](https://www.linkedin.com/in/jgliv) • [GitHub](https://github.com/lilwooce)

Kitchen Chaos VR is a fast-paced, multiplayer virtual reality cooking game inspired by *Overcooked*, designed for standalone VR headsets. Players collaborate and compete in timed cooking rounds, prepare dishes from dynamic recipes, endure unpredictable chaos events, and receive final evaluations from an AI-powered judge.

This project demonstrates advanced Unity VR systems design, real-time multiplayer networking, physics-based interaction, AI integration, and modular game architecture suitable for complex XR experiences.

---

## Project Overview

Kitchen Chaos VR combines cooperative cooking mechanics with competitive scoring and procedural disruption. Each round challenges players to assemble dishes under time pressure while environmental chaos events alter physics, object behavior, and kitchen conditions.

At the end of each round, dishes are scored deterministically based on ingredient accuracy and cooking state, then theatrically reviewed by an AI judge that delivers structured feedback and voice narration.

---

## Core Gameplay Systems

| System | Description |
|------|-------------|
| **Multiplayer Networking** | Real-time multiplayer using VelNet with persistent session management and networked avatars. |
| **Physics-Based Cooking** | Fully interactable ingredients, utensils, bowls, heat sources, and plates using rigidbodies and triggers. |
| **Recipe & Scoring Engine** | ScriptableObject-driven recipes with weighted ingredient requirements and detailed scoring breakdowns. |
| **Chaos Events Framework** | Modular chaos events that dynamically alter object physics, scale, gravity, and tool behavior mid-round. |
| **AI Dish Judge** | OpenAI-powered judge that evaluates dishes using structured JSON output and narrates results via TTS. |
| **Round & Match Flow** | Timed rounds with automatic start, player plate assignment, judging phases, and synchronized UI. |

---

## Technologies Used

### XR & Multiplayer
- Unity XR framework with OpenXR runtime  
- VelNet networking for multiplayer synchronization  
- Networked avatar spawning and transform replication  
- Persistent bootstrap and session managers  

### Interaction & Physics
- Physics-based grabbing, throwing, and tool usage  
- Trigger-based ingredient detection and plate tracking  
- Heat sources and cook state progression  
- Object tagging and layer-based interaction filtering  

### AI & Audio
- OpenAI Chat Completions API for dish evaluation  
- Structured JSON-only AI responses for deterministic parsing  
- Text-to-speech narration for judge feedback  
- Spatial audio for immersive feedback  

---

## Chaos Event System

Chaos events are implemented as modular ScriptableObjects, allowing new events to be added without modifying core gameplay logic.

Included chaos events:
- **Levitation Event**: Removes gravity from tagged objects and applies upward force  
- **Random Scale Event**: Dynamically scales kitchen objects during active chaos  
- **Rubber Knife Event**: Alters knife physics and handling characteristics  

Each event defines explicit `StartEvent` and `EndEvent` behavior, ensuring clean activation and teardown during rounds.

---

## AI Judge Pipeline

1. Dish ingredients and cook states are evaluated locally using deterministic scoring logic  
2. A structured breakdown is generated describing accuracy, missing items, and errors  
3. The AI judge receives:
   - Final numeric score  
   - Detailed scoring explanation  
4. The AI responds with strict JSON containing:
   - Qualitative feedback  
   - Ranking commentary  
   - Performance summary  
5. Results are narrated in-game using TTS for dramatic presentation  

This hybrid approach ensures gameplay fairness while enhancing immersion through AI-driven commentary.

---

## Unity & XR Configuration

- **Unity Version:** 6000.2.7f2  
- **XR Runtime:** OpenXR  
- **Build Target:** Android (standalone VR headsets)  
- **Rendering Pipeline:** URP  
- **Input:** XR Interaction Toolkit-style physics interaction  
- **Multiplayer Runtime:** VelNet  

---

## Repository Structure

```
final-project-code-uga-vr-final/
│
├── FinalProject/
│   ├── Assets/
│   │   ├── Scripts/
│   │   │   ├── Multiplayer/
│   │   │   ├── Cooking/
│   │   │   ├── Recipes/
│   │   │   ├── Chaos/
│   │   │   ├── AI/
│   │   │   └── UI/
│   │   ├── Scenes/
│   │   ├── Prefabs/
│   │   ├── Audio/
│   │   └── Materials/
│   │
│   ├── Packages/
│   └── ProjectSettings/
│
├── README.md
└── (documentation assets)
```

---

## Learning Outcomes

This project demonstrates proficiency in:

- Designing modular, scalable Unity game architectures  
- Implementing real-time multiplayer VR interactions  
- Managing complex round-based gameplay logic  
- Integrating AI services safely and deterministically  
- Building immersive physics-driven VR experiences  
- Coordinating audio, UI, networking, and gameplay systems  

---

## Summary

Kitchen Chaos VR represents a full-scale, systems-driven VR game project that blends multiplayer networking, physics-based interaction, AI-enhanced feedback, and polished round orchestration. It reflects production-level Unity XR practices and serves as a strong foundation for future VR game development, research experimentation, or portfolio demonstration.
