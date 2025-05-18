# CheapAvaloniaBlazor

An experimental project exploring integration between **Blazor Server**, **Avalonia**, **MudBlazor**, and **Photino** to build cross-platform desktop and web UIs using a shared .NET codebase.

> ⚠️ **Early Stage Development:** This project is in the very early stages. Expect rapid changes, incomplete features, and evolving structure. Contributions and ideas are welcome!

---

## 🧩 Technologies Used

- **[Blazor Server](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)** – Web UI framework running on the server.
- **[Avalonia](https://avaloniaui.net/)** – Cross-platform UI framework for desktop applications.
- **[MudBlazor](https://mudblazor.com/)** – Material Design component library for Blazor.
- **[Photino](https://www.photino.dev/)** – Lightweight, cross-platform desktop host for web-based apps.

---

## 🚧 Project Goals

- Create a **unified UI architecture** for desktop and web platforms.
- Reuse Blazor components in both **Photino** and **Avalonia** frontends.
- Showcase the flexibility of **.NET 8** and **Blazor** for cross-platform development.

---

## 🔧 Getting Started

> **Prerequisites:**
>
> - [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download)
> - Git

### 1. Clone the Repository

```bash
git clone https://github.com/CheapNud/CheapAvaloniaBlazor.git
cd CheapAvaloniaBlazor
```

### 2. Build the Project

Restore and build the source using the .NET CLI:

```bash
dotnet restore
dotnet build
```

### 3. Run the Application

Choose a project depending on the UI host you'd like to run:

```bash
dotnet run --project src/WebApp         # Blazor Server + MudBlazor
dotnet run --project src/PhotinoApp     # Photino desktop host
dotnet run --project src/AvaloniaApp    # Avalonia desktop host
```

> ⚠️ GUI apps (Photino/Avalonia) may require native dependencies based on OS. Refer to their official documentation if needed.

---

## 🗺️ Project Structure

```
src/
│
├── Shared/            # Shared models, services, and components
├── WebApp/            # Blazor Server + MudBlazor app
├── PhotinoApp/        # Desktop app using Photino
└── AvaloniaApp/       # Avalonia-based desktop shell
```

---

## 📌 Roadmap (WIP)

- [ ] Establish core layout and routing
- [ ] Enable cross-platform desktop builds
- [ ] Create shared component library (MudBlazor)
- [ ] Enable IPC or interop between Blazor and Avalonia
- [ ] Setup basic deployment pipelines

---

## 🤝 Contributing

You're welcome to explore, suggest features, or contribute:

1. Fork this repository
2. Create a new branch
3. Commit your changes
4. Open a pull request

> This is a passion project and R&D sandbox — experimentation is encouraged!

---

## 📄 License

This project is licensed under the MIT License. See the `LICENSE` file for more details.

---

## 💬 Contact

For questions, ideas, or feedback, feel free to [open an issue](https://github.com/CheapNud/CheapAvaloniaBlazor/issues).
