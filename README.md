<div align="center">
  <div>
    <a href="https://github.com/EEkebin/kebinImports">
      <img src="https://i.imgur.com/81lHW6N.png" alt="kebinImports" style="width: 35vw"/>
    </a>
  </div>

***An Importer Menu for Commonly Used/Imported Assets in Unity for Avatar/World Development in Unity VR Games***

| Social | Donate |
|:---:|:---:|
| [![Discord](https://img.shields.io/static/v1?color=blue&label=Discord&logo=Discord&logoColor=white&style=for-the-badge&message=eekebin)](https://discord.com/users/668262984463810590)<br>[![Downloads](https://img.shields.io/github/downloads/EEkebin/kebinImports/total?color=blue&style=for-the-badge)](https://github.com/EEkebin/kebinImports#README)|[![PayPal](https://img.shields.io/static/v1?color=blue&label=PayPal&logo=PayPal&style=for-the-badge&message=kebinImports)](https://paypal.me/kebinImports)<br>[![CashApp](https://img.shields.io/static/v1?color=blue&label=CashApp&logo=CashApp&logoColor=green&style=for-the-badge&message=kebinImports)](https://cash.app/$kebinImports)|
</div>

---

## **Requirements**

> kebinImports targets **Unity 2022.3.22f1**, the Unity version currently supported by VRChat, on Windows. It works in both VRChat Creator Companion projects and plain Unity projects.

## **Installation**

Every release ships the same package in three formats. All of them install under `Packages/dev.kebin.kebinimports`, never into `Assets/`, and all of them give you the same **kebinImports** menu in Unity. Pick one.

### VRChat Creator Companion / ALCOM (recommended)

kebinImports has its own VPM repository listing, so the Creator Companion can install and update it like any other package.

1. Open **[eekebin.github.io/kebinImports](https://eekebin.github.io/kebinImports/)** and click **Add to VCC / ALCOM**, or add the listing by hand: open **Settings › Packages › Add Repository**, paste `https://eekebin.github.io/kebinImports/index.json` and click **Add**. ALCOM uses the same menu and URL.
2. Open your project's **Manage Project** page, find **kebinImports** in the list and click **+**.
3. Open the project in Unity. The **kebinImports** menu is in the menu bar, and updates show up in the Creator Companion like every other package.

*Without the listing:* download `dev.kebin.kebinimports-<version>.zip` from the [latest release](https://github.com/EEkebin/kebinImports/releases/latest) and unzip it so that `package.json` ends up at `<your project>/Packages/dev.kebin.kebinimports/package.json`.

### Unity Package Manager (UPM)

1. Download `dev.kebin.kebinimports-<version>.tgz` from the [latest release](https://github.com/EEkebin/kebinImports/releases/latest).
2. In Unity open **Window › Package Manager**, click **+** in the top left and choose **Add package from tarball…**.
3. Pick the `.tgz`. Unity copies it into the project and lists kebinImports under *In Project*.

Installing from the git URL is not supported: the repository holds the sources without Unity's `.meta` files, which the build generates.

### Unity package (.unitypackage)

1. [Download `kebinImports.unitypackage` from the latest release.](https://github.com/EEkebin/kebinImports/releases/latest/download/kebinImports.unitypackage)
2. Drag it into Unity's Project window, or double-click it with the project open.
3. Click **Import**. The files land in `Packages/dev.kebin.kebinimports`. If an old copy is still in `Assets/kebinImports`, the Project Doctor offers to remove it.

## **Features**

Everything lives under **kebinImports** in Unity's menu bar, in this order.

> ***Italicized assets require purchasing to import.***

<table>
  <tbody>
    <tr>
      <th>Essentials<br>[Customizable]</th>
      <td>Avatar Essentials and World Essentials install your chosen set of tools in one go. Pick the set in Customize Essentials.</td>
    </tr>
    <tr>
      <th>Project Doctor</th>
      <td>Finds and fixes what is wrong with the project. See below.</td>
    </tr>
    <tr>
      <th>Ask kebinAI</th>
      <td>kebinAI, an AI that works inside your project. See below.</td>
    </tr>
    <tr>
      <th>Shaders</th>
      <td>Unity Toon Shader 3<br>lilToon<br>Poiyomi Toon Shader<br>Unity-Chan Toon Shader 2.0<br>Xiexe's Unity Shaders<br>Mochie's Unity Shaders<br>reroStandard Shaders</td>
    </tr>
    <tr>
      <th>Avatar Tools</th>
      <td>Modular Avatar<br>VRCFury<br>DressingTools<br>Pumkin's Avatar Tools<br>ComboGestureExpressions<br>Avatar Performance Tools<br>Gesture Manager<br>Av3Emulator<br>VRCQuestTools<br><i>Muscle Animation Editor</i></td>
    </tr>
    <tr>
      <th>World Tools</th>
      <td>VRWorld Toolkit<br>AudioLink</td>
    </tr>
    <tr>
      <th>VRChat SDK</th>
      <td>SDK 3 Avatars<br>SDK 3 Worlds<br>SDK 2 (Unity 2019 only)<br>For plain Unity projects; Creator Companion projects manage the SDK themselves.</td>
    </tr>
    <tr>
      <th>Legacy</th>
      <td><i>Dynamic Bone</i><br>Arktoon Shader<br>Cubed's Unity Shaders<br>Yukio's Fur Shader (download no longer available)<br>Greyed out unless "Show legacy items" is on in Settings.</td>
    </tr>
    <tr>
      <th>Quick Fixes</th>
      <td>Fix Materials<br>Remove Missing Scripts<br>Fix Scripting Define Symbols<br>PANIC HARD RESET ALL</td>
    </tr>
    <tr>
      <th>More</th>
      <td>Settings, Social and Donate<br>Load AssetBundle (right-click a .vrca, .vrcw or .vrcp file in the Project window › kebinImports)</td>
    </tr>
  </tbody>
</table>

> ***If any of the above assets are yours and you do not wish to allow their distribution through kebinImports, please contact me via Discord. Hopefully, we can come to a resolution or compromise.***

Every kebinImports window scales with your monitor. The **UI scale** setting in kebinImports › Settings defaults to Auto (1440p gets 125%, 4K 200%) and can be set by hand. The Unity menu bar itself is drawn by Windows, so it follows Windows display scaling or **Edit › Preferences › UI Scaling** instead.

## **How installs work**

kebinImports is a VPM client inside Unity. Every tool with an official VPM listing (Poiyomi, lilToon, Modular Avatar, VRCFury, DressingTools, Pumkin's, Thry's, Hai's, kurotu's VRCQuestTools, and VRChat's official and curated listings) is installed from that listing, in any project. The newest version your Unity supports is picked, dependencies are installed too, downloads are verified against the listing's SHA-256, and `vpm-manifest.json` is kept in sync so the Creator Companion and ALCOM can manage and update the package later. Tools without a listing come from their GitHub release or the Unity registry.

Leftovers of older install methods, such as the same tool under `Assets/` from a .unitypackage, are removed after a successful install, and materials that used the old copy's shaders are re-pointed so nothing turns pink.

kebinImports checks for updates once a day in the background and shows how many installed tools have one in its Settings window.

## **Project Doctor**

`kebinImports › Project Doctor` scans the project and offers a fix for each problem it finds:

- A tool installed twice, or installed the old way so the Creator Companion cannot update it.
- Outdated tools, packages missing from `vpm-manifest.json`, missing package dependencies, and packages nothing needs any more.
- Scripts that do not compile, materials with a missing shader, shaders with errors, and missing script components.
- A Unity version VRChat does not support.

It also knows *what* is missing. A pink material still records which shader it wants, and a missing script still records which script it was, so instead of "3 pink materials" the Doctor says "3 materials use lilToon, which is not installed" and offers to install it; instead of "missing script" it says "these objects use Modular Avatar components". It recognises the shaders and components of every tool kebinImports knows, plus Dynamic Bone.

The Doctor only looks at what the open scenes actually use, including the prefabs and materials in them, never at assets that just sit in the project. Safe fixes can be applied in one click; the rest ask first.

It also checks a Load AssetBundle preview. An avatar built for Quest shows up pink on PC, because the shaders packed inside it only work on Quest; the Doctor switches it to this project's copies of the same shaders, or installs the tool they come from (lilToon, Poiyomi, the VRChat SDK, ...). Components that don't work because the VRChat SDK is missing get the same treatment. After an install the preview reloads by itself. The preview only lives in memory, so nothing about it is saved.

## **kebinAI**

`kebinImports › Ask kebinAI` opens a chat with kebinAI, an AI that works inside your project. Ask it the way you'd ask a friend who knows Unity: "add the avatar descriptor to my avatar", "put the Hair material on the Body mesh", "add a PhysBone to my hair", "make the tail's gravity -0.8", "scale my avatar up 1.5x", "delete the sneakers". It can see the scene and your avatars, knows which bones a mesh uses (so PhysBones land on bones, not meshes), and can change any component, material, blendshape or asset (expression menus and parameters included), move, rename, duplicate and delete objects, and read the console to check its own work. It also knows kebinImports: it can install, update and remove tools, change the Essentials, run the Quick Fixes, and run the Project Doctor and apply its fixes.

kebinAI talks in plain English, not code. Before it changes anything it tells you what it wants to do, like "Install lilToon", and waits for you to allow it, and every change goes through Unity's undo history.

Pick a provider in the window's settings:

| Provider | Notes |
| --- | --- |
| Ollama, LM Studio, llama.cpp server | Local and free, no key. Use a model that supports tool calling, for example `qwen3`. kebinAI sizes Ollama's context to the conversation, so smaller GPUs work too. |
| Google Gemini (free tier) | **The best free option.** Free API key from [aistudio.google.com](https://aistudio.google.com), no credit card. Uses Gemini 3.1 Flash-Lite by default, which handles kebinAI's tools well and has the most free requests; the bigger Flash models allow only about 20 free requests a day. Google may use free-tier conversations to improve its products. |
| OpenRouter (free models) | Free key from [openrouter.ai](https://openrouter.ai/keys). `openrouter/free` picks a free model that can use tools; about 50 requests a day, and one answer can take several. |
| Groq (paid tier) | Very fast, but needs Groq's paid Developer tier: its free tier allows about 7,000 tokens per request, less than kebinAI's instructions and tools need. |
| OpenCode Zen | Your Zen API key; models bill your Zen balance. OpenCode's free models only work inside the OpenCode app itself. |
| OpenAI, Anthropic (Claude) | Your own API key. |
| Custom | Any OpenAI-compatible server (vLLM, Jan, text-generation-webui, ...). |

Base URL, key and model are remembered per provider. Keys are stored on your machine, encrypted so only your Windows account can read them, and are only ever sent to the base URL you configured.

## **Building**

There are no build scripts and no `.meta` files in the repository. GitHub Actions (`.github/workflows/build.yml`) stages the package, generates a `.meta` for every file and folder with a GUID derived from its path, and builds `kebinImports.unitypackage`, the VPM zip and the UPM tarball on every push. They can be downloaded from the workflow run's artifacts.

To release, bump `version` in `package.json`, add the matching `## v<version>` section to `CHANGELOG.md`, and push a tag with the same version, for example `git tag v2026.9.29 && git push --tags`. The workflow creates the release with the changelog section as its body, then rebuilds the VPM listing and publishes it to GitHub Pages. Pages has to be switched on once: **Settings › Pages › Build and deployment › Source: GitHub Actions**.

A second workflow (`.github/workflows/signatures.yml`) runs weekly and on demand. It regenerates the table the Project Doctor uses to recognise each tool's shaders and components, and opens a pull request when anything changed. It needs **Settings › Actions › General › Allow GitHub Actions to create and approve pull requests**.

`kebinImports.csproj` exists only so an IDE can compile the scripts against your Unity 2022.3.22f1 install; Unity itself never uses it.

## **Credits**

> **[EEkebin](https://github.com/EEkebin)  
> [ADigitalFrontier](https://github.com/ADigitalFrontier)**

## **Licenses**

> [kebinImports is licensed under the GNU General Public License v3.0.](https://github.com/EEkebin/kebinImports/blob/main/LICENSE.md)

## **Disclaimers**

> kebinImports and/or its contributors are not responsible of the actions of end users. All liabilities and responsibilities lie with the end user for the misuse(s) of kebinImports and/or any other products and/or services.

> kebinImports and/or its contributors are not sponsored by or affiliated with Unity Technologies or any of its affiliates. "Unity" is a trademark or registered trademark of Unity Technologies and/or its affiliates in the U.S. and elsewhere.

> kebinImports and/or its contributors are not sponsored by or affiliated with VRChat Incorporated or any of its affiliates. "VRChat" is a trademark or registered trademark of VRChat Incorporated and/or its affiliates in the U.S. and elsewhere.
