# Gladiator Manager Gameplay Tips & Mechanics

Extracted from `Tips.cs` in the decompiled source code.

## Attributes
- **Strength:** Determines damage dealt on a successful hit.
- **Agility:** Determines chance to dodge. Dodging is more effective against thrusts; parrying against cuts.
- **Initiative:** Determines starting control and how much control is lost on a failed attack.
- **Toughness:** Determines damage taken when hit.
- **Recovery:** Affects recovery from status effects in battle and healing speed for injuries.
- **Bravery:** Affects morale loss when wounded and the likelihood of panicking or yielding.

## Combat Mechanics
- **Armour:** Can take 100 points of damage. Higher damage increases the chance of hits getting through.
- **Injuries:** Caused by hard strikes. May lead to yielding. Some are permanent (e.g., severed hand).
- **Bleeding:** Caused by many injuries. Can lead to dizziness, unconsciousness, or death if not slowed.
- **Control:** High control increases critical hit chances. Gained more easily when low; lost quickly on failed attacks.
- **Yielding:** Can only be called by a manager after an injury. Decreases team popularity.
- **Attack Types:**
    - **Cut:** More likely to hit.
    - **Thrust:** Usually does more damage.
- **Weapons:**
    - **Clubs:** Cause blunt injuries, likely to strike the head. 'Smash' ability available with high control.
    - **Swords:** 'Precise Strike' ability available with high control, causing fatal wounds or severe bleeding.

## Management & Recruitment
- **Backer:** Determines salary and fee budgets based on team financial health.
- **Recruitment:** New gladiators available every four weeks (max 24 fighters).
- **Fighter Types:**
    - **Slaves:** Price based on physical traits; potential often unknown to slavers.
    - **Veterans:** High greed leads to high fees.
    - **Criminals:** Good temporary fodder; leave after their sentence.
    - **Citizens:** Good value, especially with high potential.
    - **Rogues:** Specialize in causing bleeding.
    - **Retarii:** Specialize in maintaining high control; use nets.

## League & Reputation
- **Divisions:** Five divisions. Top/bottom two promoted/relegated automatically; one extra spot via play-offs.
- **The Cup:** Starts week 23, six rounds. Top teams get a bye in round 1.
- **Reputation:** Increases when defeating higher reputation teams; decreases when losing to lower ones.
- **Popularity:** Based on bravery in fights. Increases if winning while wounded; decreases if yielding.
- **Attendance:** Affected by team popularity and reputation.
- **Promotion/Relegation:** Two teams each per season.
