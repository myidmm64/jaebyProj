<div align="center">

<a href="https://myidmm64.itch.io/impact-order">
  <img src="Docs/Media/Banner.jpg" alt="Impact order — a pixel-art sequence deckbuilder" width="960">
</a>

### Set your order. Land the blow.

**Developed with Codex.**

A single-player, turn-based sequence deckbuilder set in the void beneath a magic school.<br>
Arrange your cards, build Focus, and unleash your Iai strikes before the turn limit runs out.

<a href="https://myidmm64.itch.io/impact-order"><img src="https://img.shields.io/badge/itch.io-Game_page-FA5C5C?style=for-the-badge&amp;logo=itchdotio&amp;logoColor=white" alt="itch.io game page"></a>
<img src="https://img.shields.io/badge/Unity-6-222C37?style=for-the-badge&amp;logo=unity&amp;logoColor=white" alt="Unity 6">
<img src="https://img.shields.io/badge/Windows-x64-47364D?style=for-the-badge" alt="Windows x64">
<img src="https://img.shields.io/badge/WebGL-Browser-71614B?style=for-the-badge" alt="WebGL browser build">

**Windows · WebGL | English · Korean**

</div>

## The game

- **Order matters.** Fill up to six sequence slots; cards execute automatically from left to right.
- **Build the hit.** Combine Focus, flat bonuses, multipliers, and follow-up attacks.
- **Make the slot count.** Place cards in their preferred positions for extra effects, or take a chance on luck-based cards.
- **Grow your collection.** Choose a reward after each wave, merge two matching cards of the same star rank, and upgrade up to four stars.
- **Read the enemy.** Plan around visible enemy actions, card seals, and a strict turn limit.
- **3 floors · 30 battles · 34 cards · 21 items · 12 enemy types + floor bosses.**

<div align="center">
  <img src="Docs/Media/Combat.gif" alt="Late-game boss battle: cards execute in order and land a 148.33K strike" width="800">
  <br>
  <sub>Actual gameplay · Floor 3 boss battle · 2× playback</sub>
</div>

## Screenshots

<table>
  <tr>
    <td align="center" width="50%">
      <img src="Docs/Media/Inventory.png" alt="Inventory management and a selected card's description" width="440"><br>
      <sub>Manage your collection and inspect cards.</sub>
    </td>
    <td align="center" width="50%">
      <img src="Docs/Media/Sequence.jpg" alt="Late-game card arrangement with a six-card sequence" width="440"><br>
      <sub>Prepare your sequence before committing.</sub>
    </td>
  </tr>
</table>

## Controls

| Input | Action |
| :--- | :--- |
| Drag a card | Equip, reorder, merge matching cards, or return it to the bag |
| Double-click a bag card | Equip it in a free sequence slot |
| Right-click a battle bag card | Flip it to read its description |
| Right-click a sequence card | Return it to the bag |
| Right-click a card in Inventory | Sell it immediately |
| `Space` | Execute the sequence |
| `Tab` | Open or close Inventory |
| `A` / `D` | Change bag pages |
| `W` | Flip bag cards |
| `Q` / `E` | Sort / Auto merge |
| `U` | Gain the ultimate card when the gauge is full |
| `Esc` | Open the menu |
| Hover / Click an enemy's heart | Inspect items and enemy actions / Choose a target |

## Run

- **Browser:** Open the [itch.io game page](https://myidmm64.itch.io/impact-order), select **Run game**, and use fullscreen for the clearest view.
- **Windows:** Extract the entire ZIP, then launch `Impact order.exe`. Keep the Data folder, DLLs, and accompanying files together.
- **Development:** Open the project with **Unity 6000.6.4f1**, load [`Assets/Scenes/Proto.unity`](Assets/Scenes/Proto.unity), and press Play.

A mouse and keyboard are required. Change the language in **Settings**.

[Game rules](GAME.md) · [Cards](CARDS.md) · [Items](ITEMS.md) · [Enemies](ENEMIES.md) · [Balance](BALANCE.md) · [Release notes](RELEASE.md)

<details>
<summary>Project structure</summary>

| Area | Purpose |
| :--- | :--- |
| [RunEngine](Assets/01.Scripts/RunEngine.cs) | Combat rules, rewards, merging, and progression |
| [RunState](Assets/01.Scripts/RunState.cs) | Run data and owned cards |
| [GameManager](Assets/01.Scripts/GameManager.cs) | Game flow and sequence execution |
| [GameUI](Assets/01.Scripts/GameUI.cs) | Menus, inventory, and battle UI |
| [Stage](Assets/01.Scripts/Stage.cs) | Combat presentation and effects |
| [Tests](Tests) | Rule checks and balance simulations |

Built with Unity 6, Universal Render Pipeline, uGUI, and the Input System.

</details>

<details>
<summary>Credits &amp; AI disclosure</summary>

- **Developer:** [myidmm64](https://github.com/myidmm64)
- **Development:** Codex assisted with programming, artwork, and text/translation.
- **Audio:** Music and sound effects are synthesized by the game's code.
- **Font:** Galmuri by Lee Minseo (quiple) · [SIL Open Font License 1.1](Assets/04.Fonts/Galmuri-LICENSE.md).
- **Media:** Actual game captures; the combat GIF is trimmed to the start of execution and played at 2× speed.

</details>
