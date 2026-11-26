# axChat — Specification

axChat is a planned app of the axKit bundle (`docs/axKit/SPEC.md`): a conversation with a language model over a chat
API, a local server such as llama.cpp's or a service on the internet, shown and heard the way axClaude shows Claude
Code, for a blind developer who uses the NVDA screen reader. Asked by the maintainer on 2026-11-25 as a high
priority, its name and its shape (a chat app, not a general REST tool) his decision the same day; specified the same
day; **nothing is built yet** (section 8). Requirements are numbered `CH-n.m` and decisions `CH-Dn`; the bundle's
rules, AX-7 (accessibility), AX-8 (keys), AX-9 (the menus), AX-11.11 (the network) and AX-11.12 (what an AI wrote),
are cited by number and not repeated. In axChat's code and documents, "SPEC.md" means this file.

## 1. Summary

The user picks a kind of chat API by name, gives its address and, when it needs one, a key, and presses Go: one step,
since the user has already signed in to the service to get the address and the key. Then it is a conversation: the
user's message in a field, the model's reply arriving into the conversation as it is written and spoken a sentence at
a time, Escape-like keys to stop it, the conversation kept on the user's computer. Every kind of API is a named
dialect: axKit knows a few protocols (the OpenAI-compatible chat API, which llama.cpp's server, LM Studio, vLLM,
Ollama, OpenRouter and Groq all speak; Anthropic's Messages API; Ollama's own chat API) and ships presets over them,
and the user can name presets of their own. The window is the bundle's conversation window, the one axClaude and
axCalc stand on, so what the user hears and presses is what they know.

## 2. Goals and non-goals

Goals:

- G1 A conversation with any language model that a chat API serves, a local one first (llama.cpp's server on this
  computer or the local network), with the same reading, keys and sounds as axClaude's conversation.
- G2 One step to connect: a dialect by name, an address, a key when needed, Go (CH-2).
- G3 The reply heard as it is written: streamed, a sentence at a time, stoppable at once (CH-4).
- G4 Dialects as data over a few protocols in code (CH-3), so that a new server of a known kind is a preset, never a
  code change, and a new kind of API is one class of axKit's own (root rule 12).
- G5 Everything an AI wrote says so (AX-11.12): the reply is labelled as the model's, and nothing a model writes is
  ever run (CH-6).
- G6 The conversation and the key stay on the user's computer; the requests go to the address the user gave and
  nowhere else (CH-7).

Non-goals:

- A general REST client for any API and any method (a tool of its own if ever wanted, "axREST"; CH-D1).
- Messages between people (axMessage and axMail are other plans, docs/axKit/plan.md).
- A model's tool use: a tool call the model asks for is shown and never carried out (CH-6.3, CH-D6).
- Pictures, sound and files sent to the model, in the first version (section 7).
- Any sync or account of axKit's own; any telemetry.

## 3. Scenarios

1. llama.cpp's server runs on this computer. The user opens axChat (Start menu, `axKit axChat`, or the Window
   menu's Apps). The connection form: Dialect "llama.cpp server" is selected, its Address field already holds
   `http://localhost:8080`, the Key field is empty. Go: "Connected to llama.cpp server, model Qwen3 8B". The
   message field has the focus. The user types a question, Enter; the reply's sound, then its first sentence is
   spoken while the rest arrives; Done when it ends.
2. A service on the internet that speaks the OpenAI-compatible API: Dialect "OpenAI-compatible", the address the
   service's page gave, the key pasted into the hidden Key field, Go; the models the service lists are offered, the
   first selected, Enter. The connection is kept by its name for next time.
3. Anthropic's API: Dialect "Anthropic", its address filled in by the preset, the key, Go.
4. A reply runs long: Shift+Escape stops it; what came is kept, marked "stopped".
5. The user makes a preset of their own: Session, Connections, New, a name, a protocol, an address and a model, Save;
   it is in the Dialect list from then on.

## 4. Functional requirements

### CH-1 Launch and the window

- CH-1.1 axChat is an app of the bundle (`AxKit.exe chat`, the Start menu entry `axKit axChat`), added as
  `docs/axKit/adding-an-app.md` says; one window per conversation; a second start opens a new window.
- CH-1.2 The window is the bundle's conversation window, `ConversationForm` (docs/axKit/components.md): the
  conversation above (`TranscriptView`, with its reading keys, find and quick keys) and the message field under it,
  with the control area in the field's place for what the window waits on (a choice of model, a question). No second
  window (AX-7.2): the connection form, errors and questions are the window's own notices and forms.
- CH-1.3 `AxKit.exe chat <connection name>` opens straight on a saved connection; with none, the last connection
  used; with no saved connection, the connection form (CH-2).

### CH-2 Connecting, in one step

- CH-2.1 The connection form (`FormControls`, AX-8.10): Dialect (a list of the presets and the user's own, CH-3),
  Address (filled with the dialect's default address when it has one, and kept when the user changes the dialect
  after typing), Key (a hidden field; empty allowed, since a local server needs none), and the button Go, which Enter
  presses. Nothing else is asked; a model and a name come after.
- CH-2.2 Go asks the server for its models (the dialect's model list) within ten seconds. On an answer: one model is
  taken as it is, several are offered as a list in the control area with the dialect's default or the first
  selected; on a server without a model list (some llama.cpp builds serve one model and list none) the model is
  the dialect's default or none. Then "Connected to <dialect>, model <model>" and the focus in the message field.
- CH-2.3 A failure says what happened in the server's own words where it gave some, in one notice: the address
  unreachable, the key refused (401 or 403: "The key was not accepted"), not a chat API at that address (404), a
  timeout. The form comes back with what was typed, the key emptied.
- CH-2.4 The connection is kept by a name, the dialect's and the address's host at first ("llama.cpp server,
  localhost"), changeable in Session, Connections. A kept connection opens with no form. Session, Connect to... lists
  them; Session, Connections edits and removes them.
- CH-2.5 The model is changed in a conversation with Session, Choose the model (the list of CH-2.2); the change holds
  from the next message.

### CH-3 Dialects: protocols in code, presets as data

- CH-3.1 A **protocol** is one class of `AxChat.Core` for one kind of chat API (root rule 12: everything read from
  or sent to it goes through that one place): how a request is made from the conversation, how the stream is read,
  how its events become axChat's own (CH-3.4), how the model list is asked and read, how errors are read. The first
  three: `openai-chat` (POST `/v1/chat/completions`, `messages` with the roles `system`, `user` and `assistant`,
  `stream: true`, Server-Sent Events of `data:` lines ending with `data: [DONE]`, the text in
  `choices[0].delta.content`, the thinking in `delta.reasoning_content` where a server gives it, `finish_reason`,
  `usage`; models at GET `/v1/models`); `anthropic-messages` (POST `/v1/messages`, `system` apart from `messages`,
  `max_tokens` required, the headers `x-api-key` and `anthropic-version`, Server-Sent Events by event type:
  `content_block_delta` with `text_delta` or `thinking_delta`, `message_delta` with `stop_reason` and `usage`,
  `message_stop`, `error`; models at GET `/v1/models`); `ollama-chat` (POST `/api/chat`, one JSON object a line,
  `message.content`, `done`; models at GET `/api/tags`). Each is checked against recordings of the real stream
  (CH-9), never against the documentation alone.
- CH-3.2 A **dialect** is a named preset over a protocol, a small JSON file: its name, its protocol, a default address,
  the key's header and form where the protocol allows a choice (`Authorization: Bearer <key>` for openai-chat), any
  fixed headers and body members (a `max_tokens`, a `temperature`), a default model, and words for the user (what
  it is for, where its key is found). The presets ship in the program: "llama.cpp server" (openai-chat,
  `http://localhost:8080`), "OpenAI-compatible" (openai-chat, no address), "Ollama" (ollama-chat,
  `http://localhost:11434`), "Anthropic" (anthropic-messages, `https://api.anthropic.com`). The user's own are files
  in `Documents\axKit\axChat\dialects` (AX-5.11), made and changed in Session, Connections, New dialect, or in axJSON.
- CH-3.3 A dialect that does not read, or names a protocol axChat does not have, is listed with why and cannot be
  chosen; nothing in a dialect is ever run, and a dialect names no program.
- CH-3.4 **axChat's own events**: every protocol's stream becomes the same few: `TextDelta`, `ThinkingDelta`,
  `ToolCallAsked` (the tool's name and its input as text), `Finished` (why: the model ended, the length limit, the
  user stopped it, a refusal), `Usage` (tokens in and out), `Failed` (the error's words). The window knows these and
  nothing of any API. What reaches the user is raised as kinds of event at the door (root rule 13, `EventKind`):
  a reply arriving, Done, Failed; a new moment gets its kind first.

### CH-4 The conversation

- CH-4.1 The message field as axClaude's: Enter sends, Shift+Enter a new line; the field empties when the message is
  sent; Ctrl+Up and Ctrl+Down bring back the messages sent before.
- CH-4.2 The conversation's lines as axClaude's, with the same jump keys: the user's message under `you:`, the
  model's reply under the label of CH-4.3, the exchange markers between turns, a heading of the reply a heading
  (FR-3 of axClaude, its Markdown made plain by the same rules as `ClaudeCodeMarkdown`'s, which move to the shared
  pieces on this second use, root rule 4).
- CH-4.3 The reply's label says whose words they are (AX-11.12): `ai:` (section 7: the word is the maintainer's to
  hear). The thinking, where a model gives it, is under `thinking:`, hidden until Options, Show the model's thinking.
- CH-4.4 Arriving: the reply's sound as its first words arrive; the text goes into the conversation as it comes, a
  line at a time, never moving the caret (AX-7, rule 1); it is spoken a sentence at a time, joined as axClaude joins
  a reply's lines (`Arrivals` and `Utterance`), and not at all while the user reads elsewhere and has turned
  Options, Speak replies off. Done when it ends, after its last words.
- CH-4.5 Stopping: Shift+Escape (axClaude's interrupt key) cancels the request at once; what came stays, followed by
  the line "stopped"; "Stopped" is said. Escape in the field is the guard axClaude has (it only speaks).
- CH-4.6 The end said when it is not the model's own: "Cut off at the length limit", "The model refused", "Failed:
  <the server's words>". A limit of requests (429) says how long the server asks to wait, and nothing is sent
  again by itself.
- CH-4.7 On demand, never spoken by itself: the model, the tokens of the last reply and of the conversation (the
  status key, F8 as in axClaude), how long the reply took.
- CH-4.8 A new conversation (Ctrl+N in Session) starts empty on the same connection; Save conversation as text
  writes the conversation as the view shows it.

### CH-5 The history and the context

- CH-5.1 The APIs remember nothing between requests: every message is sent with the whole conversation before it,
  as the protocol shapes it, after the connection's system prompt (Session, Connections; empty at first).
- CH-5.2 When the server says the conversation is longer than the model takes, axChat says so in one notice and
  offers to send it again without the oldest exchanges, saying how many would be left out; it never shortens a
  conversation without the user's word.
- CH-5.3 Conversations are kept in `Documents\axKit\axChat\conversations`, one JSON file each, named by its first
  message and the date, and are opened again from Session, Open a conversation, where they go on. Options, Keep
  conversations turns the keeping off (section 7: on or off at first is the maintainer's choice). A kept
  conversation holds the dialect's and the model's names, never the key or the address's query.

### CH-6 What the model wrote says so (AX-11.12, root rule 11)

- CH-6.1 The first time a connection is used, and in the F1 text, the caution of `AxKit.Core/AiCaution.Written`: the
  replies are a model's, can be wrong, and axKit checked nothing of them.
- CH-6.2 A reply's code is text: nothing in a reply is run, opened or sent anywhere by axChat; copying it is the
  user's act.
- CH-6.3 A tool call the model asks for (`ToolCallAsked`) is shown as a line, "The model asked to use <tool>; axChat
  runs no tools", and the turn ends there (CH-D6). Tools are a later version, designed with their caution from the
  start, never a default.

### CH-7 The network and the key (root rule 8, AX-11.11)

- CH-7.1 axChat is the third place where axKit's own code makes network requests (after the updater and the report
  sender): to the address of the connection the user chose, and nowhere else; no request is made before Go, and none
  while the window is idle. Every request is the one HTTP client's (`AxKit.Core/AxKitHttp`) and carries its
  `Outside call` comment (B-17). `PRIVACY.md`, `README.md`, `docs/axKit/README.md`, `SECURITY.md`, AX-11.11 and
  `tools/outside-calls.ps1`'s tables change in the build's first commit that makes a request.
- CH-7.2 An address with `http://` (no encryption) is taken for this computer and the local network alone
  (localhost, a private address range, a `.local` name); to any other address with a key it is refused with why, so
  that a key never crosses the internet in the clear.
- CH-7.3 The key is kept for the Windows account alone (`AxKit.Core/UserSecret`, DPAPI), in a file of its own per
  connection, never in a settings file, never written to the log, never shown again (the form shows "a key is kept;
  type a new one to change it"), and never in a report (F4) or Copy diagnostics.
- CH-7.4 The log holds numbers and fixed words only (a status code, a duration, a count of tokens), never a message,
  a reply, an address's path or query, or a key.

### CH-8 Menus, keys, texts, settings

- CH-8.1 The bundle's menus (AX-9.1): Session (New conversation, Open a conversation, Connect to..., Connections,
  Choose the model, Save conversation as text, Close), Context, Options (Speak replies, Show the model's thinking,
  Keep conversations, the sounds), Window, axKit, Help.
- CH-8.2 Every key in `AxChatKeys`, one table (AX-8); the conversation's keys are the conversation window's own and
  the same as axClaude's wherever they do the same thing.
- CH-8.3 Settings in `%APPDATA%\AxKit\axChat.json` (the connections by name, dialect, address and model; the
  options); the keys and the conversations elsewhere, as above.

### CH-9 Tests

- CH-9.1 Each protocol is tested from recordings of the real stream of a real server (llama.cpp's first), kept under
  `tests/fixtures/chat/<protocol>`, recorded with a key never written into them and read for personal data before
  they are committed (rule 6): the requests it makes for a conversation, each stream read into axChat's events,
  the errors.
- CH-9.2 Window scripts on the hidden desktop against a stand-in server of the runner's own on this computer, which
  plays a recorded stream: connect, a reply heard, stop, a failure, a kept connection; the live check against a real
  llama.cpp server joins `tools/upstream.ps1` as llama.cpp's entry.

## 5. Architecture

- `src/AxChat.Core` (pure, unit tested): `ChatDialect` and `Dialects` (the presets and the user's files read,
  CH-3.2, CH-3.3), the protocols (`OpenAiChat`, `AnthropicMessages`, `OllamaChat`: a request from a `Conversation`,
  a stream line read into `ChatEvent`s, a model list, an error), `StreamLines` (Server-Sent Events and one object a
  line, read from bytes as they come), `ChatEvent` (CH-3.4), `Conversation` (the turns, the shortening of CH-5.2,
  the file of CH-5.3), `Connections` (the settings' side), `AddressRule` (CH-7.2).
- `src/AxChat` (the window): `ChatForm` on `ConversationForm`, the connection form, the request run off the UI
  thread through `AxKitHttp` with its cancellation, the events posted to the window and batched as axClaude's
  frames are (AX-7, rule 3).
- Shared pieces that move on this second use, one per commit (root rule 4): the Markdown made plain
  (`ClaudeCodeMarkdown`'s rules, to `AxKit.Core` as a neutral piece, axClaude's keeping its name for Claude Code's
  quirks if any) and whatever of axClaude's reply speech the window needs.
- `tests/AxChat.Tests`, `tests/fixtures/chat`, `tests/scripts/axChat`, `docs/axChat` (this file, `user-guide.md`,
  `nvda-test-plan.md`), as `docs/axKit/adding-an-app.md` lists.

## 6. Design decisions

- **CH-D1 A chat app, not a general REST client** (the maintainer, 2026-11-25, after the analysis). A chat API's
  stream, the conversation the client keeps and resends, a reply's parts (thinking, tool calls, why it stopped,
  tokens) and the model list need a window that knows them; a client for any request would show JSON and wait for
  the whole answer. A general REST tool stays possible as an app of its own.
- **CH-D2 The name axChat** (the maintainer, 2026-11-25): it says what one does in it; a chat between people would be
  axMessage.
- **CH-D3 Protocols in code, dialects as data.** A general mapping language from a schema to requests and events
  would be clever code a smaller model cannot change safely (B-10); the chat APIs are few and share most of their
  shape, so a protocol is a plain class tested from recordings, and everything that differs between servers of one
  protocol (address, headers, fixed members, model) is data the user can write.
- **CH-D4 The conversation window** (docs/axKit/framework.md: new work on a window type, never a window of its own),
  so that the reading, the jump keys and the sounds are axClaude's.
- **CH-D5 A third network contact of axKit's own**, made only to the address the user chose, after Go; the rule-8
  documents change with the first build that makes one (CH-7.1).
- **CH-D6 No tool use in the first version.** A model's tool call asks to act on the user's computer or elsewhere;
  that needs its own design with its caution (rule 11), and a local model's tool calls are its weakest part.
- **CH-D7 The key for this Windows account alone** (DPAPI through `UserSecret`, as the AI tools' key is kept).

## 7. Open questions (for the maintainer, and for the design sitting with Fable)

1. The reply's label: `ai:` (proposed), the dialect's or the model's name, or `model:`. **Decided on 2026-11-25:
   `ai:`**, the same for every model and server; Information says which model answered.
2. Conversations kept at first, or off until turned on (the bundle's rule of private defaults would say off).
   **Decided on 2026-11-25: off until turned on.**
3. Whether AI help (Shift+F1, axHelp) may later use axChat's connections as its service, beside Claude Code (the
   parked idea "axHelp and AI help for another AI service", plan.md). **Decided on 2026-11-25: yes, later**, a
   choice in AI help's settings once axChat works, Claude Code the default.
4. Pictures and files sent to a model that takes them; a model's tools (CH-D6), each a design of its own.
5. Whether a reply is spoken a sentence at a time (proposed) or as each line ends, by ear.
6. The sounds: a reply arriving and Done as axClaude's, or sounds of their own, by the maintainer's ear (rule 13).

## 8. Status

Specified 2026-11-25; nothing built. The build, in stages, each with its tests and checks: (1) `AxChat.Core` with the
openai-chat protocol and its recordings from a real llama.cpp server; (2) the window with one connection made in the
form, a reply heard and stopped, the rule-8 documents changed in the same commit; (3) the kept connections, the
model list and the presets; (4) the kept conversations and the context's shortening; (5) the Anthropic and Ollama
protocols, the user's own dialects; then the NVDA pass. The priorities list's item "axChat (name to settle)" holds
the analysis this specification came from.
