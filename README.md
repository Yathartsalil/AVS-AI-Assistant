# AVS — Multi-Model AI Chat

<div align="center">

![AVS Banner](https://img.shields.io/badge/AVS-Multi--Model%20AI-6366f1?style=for-the-badge&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Firebase](https://img.shields.io/badge/Firebase-FFCA28?style=for-the-badge&logo=firebase&logoColor=black)
![Ollama](https://img.shields.io/badge/Ollama-Local%20AI-black?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)

**AVS is a full-stack AI chat application that runs three AI models simultaneously — synthesizing their responses into one perfect answer.**

[Features](#features) · [Demo](#demo) · [Installation](#installation) · [API Keys](#api-keys-setup) · [Firebase Setup](#firebase-setup) · [Contributing](#contributing)

</div>

---

## What is AVS?

AVS (AI Vision System) is a self-hosted, multi-model AI chat platform built with **ASP.NET Core** and **Firebase**. Instead of relying on a single AI, AVS queries multiple models in parallel and synthesizes the best possible answer — or lets you choose the right tool for the job.

Users sign in with email/password or Google, and their full chat history is synced to Firestore — accessible from any device.

---

## Features

- **Three AI Modes**
  - `VS-1 Basic` — Fast, private answers powered by Ollama running locally on your machine
  - `VS-G1 Image` — AI image generation via Hugging Face FLUX.1-schnell
  - `VS-2 Pro` — Queries Groq (Llama 3.3 70B), Gemini 2.5 Flash, and Ollama simultaneously, then synthesizes all three into one response

- **User Accounts**
  - Email + Password sign up / sign in
  - Google OAuth
  - Welcome email sent on account creation via Gmail SMTP

- **Cross-Device Chat History**
  - All sessions stored in Firebase Firestore
  - Load your conversations from any device, any browser

- **File Uploads**
  - Attach images, PDFs, text files, and code files to any message
  - File contents injected into the AI prompt automatically

- **Session Memory**
  - Full conversation context sent with every message
  - AI remembers everything said earlier in the session

- **Professional UI**
  - Dark theme with a clean, modern design
  - Collapsible source cards showing each model's individual response in Pro mode
  - Animated thinking indicators, progress bar, suggestion chips

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core (.NET 8), C# |
| Frontend | Vanilla HTML/CSS/JS (single file) |
| Local AI | Ollama + llama3.2 |
| Cloud AI (text) | Groq API (Llama 3.3 70B) + Google Gemini 2.5 Flash |
| Cloud AI (image) | Hugging Face Inference API (FLUX.1-schnell) |
| Auth | Firebase Authentication |
| Database | Firebase Firestore |
| Email | Gmail SMTP |

---

## Installation

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Ollama](https://ollama.com) installed and running
- A Firebase project (see [Firebase Setup](#firebase-setup))
- API keys for Groq, Google Gemini, and Hugging Face (see [API Keys Setup](#api-keys-setup))
- A Gmail account with an App Password

### 1. Clone the repository

```bash
git clone https://github.com/YatharthCoding/AVS.git
cd AVS
```

### 2. Pull the local AI model

```bash
ollama pull llama3.2
```

### 3. Set your environment variables

```bash
export GOOGLE_API_KEY="your_gemini_api_key"
export GROQ_API_KEY="your_groq_api_key"
export HF_API_KEY="your_huggingface_api_key"
```

To make these permanent, add them to your `~/.bashrc`:

```bash
echo 'export GOOGLE_API_KEY="your_key"' >> ~/.bashrc
echo 'export GROQ_API_KEY="your_key"' >> ~/.bashrc
echo 'export HF_API_KEY="your_key"' >> ~/.bashrc
source ~/.bashrc
```

### 4. Start Ollama

```bash
ollama serve
```

### 5. Run the app

Open a new terminal in the project folder:

```bash
dotnet run
```

### 6. Open in browser

```
http://localhost:5000
```

---

## API Keys Setup

### Groq (Free)
1. Go to [console.groq.com](https://console.groq.com)
2. Sign up and go to **API Keys**
3. Create a new key and copy it

### Google Gemini (Free tier available)
1. Go to [aistudio.google.com](https://aistudio.google.com)
2. Click **Get API Key** → Create API key
3. Copy the key

### Hugging Face (Free)
1. Go to [huggingface.co](https://huggingface.co) and create a free account
2. Go to **Settings** → **Access Tokens** → **New Token**
3. Set type to **Write** and copy the token

### Gmail App Password
1. Go to [myaccount.google.com](https://myaccount.google.com) → **Security**
2. Enable **2-Step Verification** if not already on
3. Search for **App Passwords** → create one for AVS
4. Copy the 16-character password and update it in `Program.cs`

---

## Firebase Setup

### 1. Create a Firebase project
1. Go to [console.firebase.google.com](https://console.firebase.google.com)
2. Click **Add Project** → name it → continue
3. Go to **Project Settings** → **General** → scroll to **Your apps** → click **Web** (`</>`)
4. Register the app and copy the `firebaseConfig` object
5. Paste it into `wwwroot/index.html` where the Firebase config is initialized

### 2. Enable Authentication
1. In Firebase Console → **Authentication** → **Sign-in method**
2. Enable **Email/Password**
3. Enable **Google** — add your email as support email → Save

### 3. Create Firestore Database
1. Firebase Console → **Firestore Database** → **Create database**
2. Start in **test mode** (you will secure it in the next step)

### 4. Deploy Firestore Security Rules

Install Firebase CLI if you haven't:

```bash
curl -sL https://firebase.tools | bash
firebase login
```

Then from your project directory:

```bash
firebase init firestore
firebase deploy --only firestore:rules
```

The `firestore.rules` file should contain:

```
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    match /users/{userId}/{document=**} {
      allow read, write: if request.auth != null && request.auth.uid == userId;
    }
  }
}
```

---

## Project Structure

```
AVS/
├── Program.cs              # ASP.NET Core backend — all API routes and AI services
├── AVS.csproj              # Project file and NuGet dependencies
├── firestore.rules         # Firestore security rules
├── firebase.json           # Firebase project config
└── wwwroot/
    └── index.html          # Entire frontend — auth UI, chat UI, Firebase integration
```

---

## How It Works

```
User sends message
       │
       ▼
  Mode selected?
  ┌────┴────────────┐
  │                 │
VS-1 Basic      VS-G1 Image      VS-2 Pro
  │                 │                │
Ollama          HuggingFace     Groq + Gemini + Ollama
llama3.2        FLUX.1-schnell  (parallel)
  │                 │                │
  └────────────────►▼◄───────────────┘
                Response
           (Pro: synthesized
            by Gemini 2.5 Flash)
                   │
                   ▼
           Saved to Firestore
           (per user, per session)
```

---

## Contributing

Contributions are welcome! Here's how to get started:

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature`
3. Make your changes and commit: `git commit -m "Add your feature"`
4. Push to your fork: `git push origin feature/your-feature`
5. Open a Pull Request

Please make sure your code follows the existing style and that all three modes (Basic, Image, Pro) still work before submitting.

---

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

---

<div align="center">
  Built with ❤️ by <a href="https://github.com/YatharthCoding">YatharthCoding</a>
</div>
