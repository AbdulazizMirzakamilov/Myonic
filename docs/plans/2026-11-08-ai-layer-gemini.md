# AI Layer (Google Gemini) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement Sections 23–26 and 31 of the project specification (ТЗ): local, user-configured Google Gemini AI analytics layer for post-workout insights and exercise progression recommendations, with 100% offline isolation.

**Architecture:** Client-side REST integration with Google Gemini API (`gemini-2.5-flash` / `gemini-1.5-flash`) via `HttpClient`. All prompt generation is done purely from local SQLite workout history. API keys are stored locally. Failure handling guarantees the app remains completely functional when offline or without an API key.

**Tech Stack:** .NET 10 MAUI, C# 13, SQLite-net-pcl, System.Text.Json, HttpClient, CommunityToolkit.Mvvm.

**Spec:** Project Specification Разделы 23–26, 31.

## Global Constraints
- Core strength-tracking and history functions must remain 100% offline and never block or fail due to AI.
- Design: Kinetic Obsidian dark theme (#C4F135 volt accent, dark surfaces #121214, #1E1E22).
- Security: User API key stored securely in local device preferences/storage and never sent anywhere except directly to Google's official Gemini endpoint.

---

### Task 1: Core Models, Interfaces & Prompt Builder
- [ ] Create `Myonic.Core/Models/AiAnalysisModels.cs` with:
  - `AiAnalysisResult`
  - `AiConnectionTestResult`
  - `AiPromptBuilder` for Workout Session and Exercise Progress prompts
- [ ] Create `Myonic.Core/Services/IAiSettingsService.cs`
- [ ] Create `Myonic.Core/Services/IAiAnalysisService.cs`

### Task 2: Service Implementation
- [ ] Create `Myonic.App/Services/AiSettingsService.cs`
- [ ] Create `Myonic.App/Services/GeminiAiAnalysisService.cs`

### Task 3: Unit Tests
- [ ] Create `Myonic.Tests/AiPromptBuilderTests.cs`
- [ ] Create `Myonic.Tests/GeminiAiAnalysisServiceTests.cs`
- [ ] Verify tests pass with `dotnet test Myonic.Tests`

### Task 4: Settings Integration
- [ ] Update `SettingsViewModel.cs` with AI settings and connection test commands
- [ ] Update `SettingsPage.xaml` with Gemini API configuration card

### Task 5: Workout Session Details Integration
- [ ] Update `WorkoutSessionDetailViewModel.cs` with `AnalyzeWithAiCommand`
- [ ] Update `WorkoutSessionDetailPage.xaml` with AI analysis card and button

### Task 6: Exercise Details Integration
- [ ] Update `ExerciseDetailViewModel.cs` with `AnalyzeProgressWithAiCommand`
- [ ] Update `ExerciseDetailPage.xaml` with AI progress insights card and button

### Task 7: Dependency Injection & Verification
- [ ] Register AI services in `MauiProgram.cs`
- [ ] Run all unit tests
- [ ] Compile for Android and Windows targets
- [ ] Commit and push to git
