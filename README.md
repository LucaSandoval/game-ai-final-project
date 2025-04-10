# TAGGERS! - Autonomous Models for Player Perception and Pathfinding

## CS5150 Final Project

### Overview

Agent pathfinding and perception have long been topics of interest for artificial intelligence research, especially for their use in robotics, autonomous vehicles, and video games; age-old pathfinding algorithms like Dijkstra’s and A-Star are still commonly used for exploring the surroundings of an environment, while perception systems can vary wildly based on an AI’s specific intent and purpose. 

As such, we present TAGGERS!, a tag simulator that demonstrates our knowledge of the inner workings of AI pathfinding and perception agents. Using 4 different behavior models, we showcase the Game AI topics of movement and perception through the medium of a single-player 2D reflex game. TAGGERS! is fully-playable with player controls, menu screens, and a win/loss condition; we extend what we’ve learned in CS5150 to improve our tagging systems with predictive movement, information searching, and advanced group behavior.


<img width="900" alt="TitleScreen" src="https://github.com/user-attachments/assets/addf14a2-7270-4b22-827d-b43a14c7459c" />

### AI Systems

Along with a translation and reinforcement of the concepts we’ve learned in class, we’ve extended our AI features with additional systems and features.

For its AI systems, TAGGERS! implements:
- A grid/tile system, complete with world-to-grid helper methods and documentation
- Dijkstra’s algorithm & A-Star for pathfinding 
- Spatial Functions for determining agent behavior
- A player perception system via occupancy maps & tile ranking
- An information sharing system, complete with user-perceptive “Barks”
- Toggleable predictive path movement
- Pack Searching

### Game Systems
For its game systems, TAGGERS! Implements:
- A player controller, complete with a sprint mechanic and stamina system
- A timer and scoring system. The player must collect the stated amount of “treasure”(green) tiles in the allotted time limit. 
- A win/loss condition. The player wins if they collect the listed amount of treasure tiles and survives for the duration of the time limit. Otherwise, they lose. 
- UI menus for selecting levels, winning, and losing. 
- Multiple levels for play, along with an intuitive level editor in Unity. 
- Audio and sound effects.

### Behavior Models

TAGGERS! features 4 unique behavior models for the taggers in its levels, each with their own strategies for finding and tagging the player. 

These behavior models are: 
- The blue “Chaser”: Fast and precise. Aims to collide into the player in as little time as possible, forgoing any attempt at prediction or cooperation with their peers.
- The green “Predictor”: Experienced and calculating. Predicts the player’s movement in an attempt to cut them off. 
- The purple “Blocker”: Slow and defensive. Hangs away from the player to prepare for any escape attempts against the other taggers.  
- The red “Stalker”: The player’s shadow. Doesn’t want to be seen, preferring to attack with the element of surprise. 

Through the process of building TAGGERS!, we’ve honed our understanding of Game AI systems, creating a game from scratch, and translating game concepts across frameworks and engines. We hope you enjoy playing it! 


### Authors
- Carlos Gaza
- Luca Sandoval
- Lucas Dunker
- Kevin McGrath
- Nick Law


