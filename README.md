# AI Interview Assistant

AI-powered desktop assistant designed to help during technical interviews and online assessments.

## Overview

AI Interview Assistant is a Windows desktop application built with WPF and .NET.

The application provides an overlay-based interface for interview assistance, AI-generated answers, voice input, online test question capture, and ChatGPT WebView integration.

## Features

- AI-powered interview question answering
- ChatGPT WebView integration
- Online test question capture
- Vision-based question processing
- Voice input and speech-to-text
- Local voice processing support
- Configurable AI models
- Configurable answer modes
- Resume and candidate profile context
- Customizable hotkeys
- Dark / Light / System appearance modes
- Adjustable window opacity
- Screen capture protection / privacy mode
- Chat history management
- Smooth chat scrolling
- Typing animation
- Configurable AI timeout and retry settings
- Settings persistence

## Technology Stack

- C#
- .NET Framework / .NET
- WPF
- WebView2
- REST APIs
- OpenRouter
- Whisper
- NAudio
- Tesseract OCR
- Newtonsoft.Json
- JavaScript

## Project Structure

```text
AiInterviewAssistant
│
├── AiInterviewAssistant.sln
│
├── AiInterviewAssistant
│   ├── MainWindow
│   │   ├── AI
│   │   ├── OnlineTests
│   │   └── Voice
│   │
│   ├── ChatGPTHost
│   │   └── Scripts
│   │
│   ├── Settings
│   │   ├── AI
│   │   ├── Appearance
│   │   ├── General
│   │   ├── Resume
│   │   └── Voice
│   │
│   ├── Security
│   ├── Native
│   └── Properties
│
└── KeyEncryptor