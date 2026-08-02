# Product Overview

## Product Vision

The product is a **multi-session conversational AI platform for creating, configuring, operating, and improving reusable AI agents across structured activities involving multiple participants**.

Administrators can define an agent’s identity, responsibilities, knowledge, behavior, tools, memory configuration, and evaluation approach, then deploy that agent into controlled activities such as assessments, interviews, coaching sessions, project reviews, requirements discovery, customer support, onboarding, and other guided conversational processes.

The platform provides a chat-like and voice-enabled interaction experience with:

* Text conversation
* Streaming speech-to-text
* Streaming text-to-speech
* Natural, interruptible voice interaction
* Participant submissions and attachments
* Multiple concurrent and isolated participant sessions
* Individual or explicitly configured group sessions
* Time, deadline, and attempt controls
* Structured workflows and session stages
* Adaptive follow-up questions
* Agent tools and knowledge access
* Configurable memory modes
* Evidence collection
* Structured evaluations and outcomes
* Human-review workflows
* Reproducible agent behavior
* Harness snapshots, comparison, backup, and restoration
* Comprehensive transcript, event, evidence, and audit tracking

The first MVP focuses on **AI-assisted assessment and examination**, while the underlying architecture remains a general-purpose foundation for structured conversational agents.

The assessment use case is the initial product experience rather than a limitation of the platform model. The same agent, harness, session, workflow, memory, and evaluation concepts can support:

* Candidate interviews
* Employee coaching
* Speaking practice
* Project and design reviews
* Requirements gathering
* Customer onboarding
* Customer-support conversations
* Compliance interviews
* Knowledge checks
* Guided investigations
* Structured feedback sessions
* Professional certification activities
* Other time-bounded or workflow-controlled conversations

The platform is designed around controlled flexibility. Agents may improve over time when memory and harness learning are enabled, while administrators retain visibility, approval authority, auditability, reproducibility, and the ability to restore earlier configurations.

## Core Product Principles

The product separates the agent’s identity, the operating system around the agent, the activity configuration, and the individual participant interaction.

> **Agent defines who the AI is, what it knows, what it can do, how it behaves, and how it reasons about outcomes.**

> **Harness defines how the agent operates within a controlled process, including workflows, policies, tools, rubrics, validation rules, memory controls, and execution procedures.**

> **Campaign defines a structured activity, including its participants, tasks, limits, rules, and selected agent and harness configuration.**

> **Session represents one isolated interaction between the configured agent and a participant or explicitly configured participant group.**

These concepts are related but intentionally distinct.

An agent should not need to be recreated for every assessment, interview, coaching program, or support process. The agent remains reusable, while its operating harness and activity configuration determine how it behaves in a particular context.

A campaign is one structured deployment and coordination mechanism within the platform. It does not define the limits of the underlying agent model. The platform foundation should remain suitable for other forms of agent deployment, orchestration, and structured interaction as the product evolves.

## Agent

An **Agent** is a reusable AI identity configured and managed by an administrator.

The agent represents the durable conceptual entity that participants interact with. It defines the agent’s role, capabilities, knowledge, behavioral expectations, and decision-making approach independently of any single campaign or session.

An agent may define:

* Name and identity
* Role and responsibilities
* Persona and communication style
* Behavioral instructions
* Domain knowledge
* Context and reference material
* Available skills
* Available tools
* Tool-use principles
* Default procedures
* Interaction style
* Questioning approach
* Safety boundaries
* Operational boundaries
* Escalation behavior
* Evaluation or decision-making behavior
* Evidence requirements
* Expected output formats
* Confidence and uncertainty behavior
* Human-review requirements
* Default memory mode
* Memory eligibility and retention controls
* Learning and improvement policies

Examples of agents include:

* Examiner
* Interviewer
* Speaking coach
* Project reviewer
* Requirements analyst
* Customer-support agent
* Onboarding guide
* Compliance reviewer
* Technical mentor
* Research interviewer
* Performance coach
* Training facilitator

The same agent may be used across many activities while retaining a recognizable role and consistent operating identity.

For example, a software-engineering examiner agent may be used for several assessments, institutions, participant groups, or examination formats. Its role and evaluation philosophy can remain stable while different harness configurations and campaign rules define the exact workflow, rubric, tools, time limits, and participant requirements.

## Agent Memory Modes

Administrators can configure whether an agent may learn from approved interactions and operational experience.

The platform supports two primary memory modes.

### Dynamic Agent Mode

In **Dynamic Agent Mode**, memory is enabled.

The agent may learn from approved sources such as:

* Completed participant interactions
* Human feedback
* Reviewed evaluations
* Corrected outputs
* Operational outcomes
* Administrator guidance
* Approved reviewer comments
* Repeated workflow issues
* Tool-use results
* Validated best practices
* Approved domain knowledge

Dynamic mode allows the agent to improve its future behavior, subject to configured memory policies and administrative controls.

Potential improvements may include:

* Better questioning strategies
* Improved explanations
* More reliable tool selection
* Stronger evaluation consistency
* Awareness of recurring edge cases
* Better handling of uncertainty
* More effective follow-up questions
* Improved interaction pacing
* More accurate recognition of evidence
* Better adherence to organizational practices

Dynamic mode does not permit uncontrolled learning from all participant content.

Memory updates must respect:

* Participant isolation
* Data-use permissions
* Campaign and session policies
* Administrator approval settings
* Privacy constraints
* Evidence requirements
* Retention rules
* Audit requirements
* Organizational boundaries

Participant information must not be reused across unrelated participants, sessions, campaigns, or organizations unless explicitly permitted.

### Stable Agent Mode

In **Stable Agent Mode**, memory is disabled for new long-term learning.

The agent continues to use its configured identity, knowledge, tools, instructions, approved existing memory, and harness configuration, but it does not create new persistent memories from the current interaction.

Stable mode is useful when:

* Consistency is more important than adaptation
* Assessments must remain reproducible
* Participants must receive equivalent treatment
* Regulatory or audit requirements restrict learning
* A controlled experiment is being conducted
* A configuration must remain unchanged
* The agent is being benchmarked
* Administrators want to validate a specific harness state
* Participant data must not contribute to future behavior

Disabling memory prevents new long-term learning. It does not automatically delete previously approved memories.

Administrators may separately inspect, edit, approve, archive, deactivate, or delete stored memories.

### Memory Configuration Scope

Memory mode may be configured at multiple levels:

* Agent level
* Harness level
* Campaign level
* Session level

An agent may have a default memory mode, while a campaign or session may override that default when administrative policy permits.

For example:

* A coaching agent may normally operate in dynamic mode.
* A certification assessment may override it to stable mode.
* A pilot session may enable limited learning only from reviewer-approved outcomes.
* A sensitive participant session may disable all persistent memory writes.

Each session must record the exact memory mode and memory policy used during that interaction.

## Agent Memory Management

Agent memory must be visible and administratively controlled rather than treated as an opaque, unrestricted model capability.

Administrators should be able to:

* Inspect stored memories
* Review the source of a memory
* See when and why it was created
* Identify the agent and harness that created it
* Identify the approval status
* Edit inaccurate memories
* Approve proposed memories
* Reject proposed memories
* Archive outdated memories
* Delete inappropriate memories
* Restrict memory applicability
* Define retention periods
* Control which campaigns may use particular memories
* Prevent participant-specific information from being generalized
* Review memory-use history
* Disable future memory creation

A memory may contain:

* A validated operational lesson
* A preferred interaction pattern
* A confirmed domain fact
* A reviewer-approved correction
* A known failure case
* A reliable tool-use strategy
* A policy interpretation
* An evaluation calibration example
* A reusable explanation
* A participant-specific fact when explicitly permitted

The system should distinguish between:

* Agent-level operational memory
* Domain knowledge
* Participant-specific memory
* Campaign-specific context
* Session-only working context
* Harness improvement proposals
* Evaluation calibration data

These categories have different privacy, retention, approval, and reuse requirements.

## Harness

A **Harness** defines how an agent operates.

It surrounds the agent with the instructions, procedures, policies, workflows, tools, validation behavior, memory controls, and evaluation mechanisms required for a particular class of activities.

The harness may contain:

* System instructions
* Role-specific instructions
* Persona guidance
* Context and knowledge sources
* Skills
* Tool definitions
* Tool permissions
* Tool-use procedures
* Workflows
* Session stages
* Transition rules
* Rubrics
* Evaluation procedures
* Evidence requirements
* Policies
* Safety constraints
* Validation rules
* Output schemas
* Completion requirements
* Escalation rules
* Human-review rules
* Conversation protocols
* Voice and interruption policies
* Time-management behavior
* Memory controls
* Learning policies
* Confidence thresholds
* Failure-handling procedures

The agent and harness are related but not interchangeable.

The agent defines the reusable AI identity and capability model. The harness defines the controlled operating environment in which that agent performs a structured activity.

For example, the same interviewer agent could operate under:

* A technical interview harness
* A behavioral interview harness
* A structured research interview harness
* A graduate recruitment harness
* A coaching-oriented mock interview harness

Each harness may use different workflows, tools, question rules, scoring models, evidence requirements, and completion conditions without requiring a completely new agent identity.

## Harness Learning and Improvement

The harness is **mutable** and may improve over time.

Harness improvements may update:

* Instructions
* Workflow steps
* Transition conditions
* Tool permissions
* Tool-use procedures
* Rubrics
* Scoring guidance
* Evidence rules
* Validation checks
* Output schemas
* Safety policies
* Escalation rules
* Interaction policies
* Memory controls
* Evaluation behavior
* Error-handling procedures
* Human-review criteria

Harness improvements may be:

* Made manually by an administrator
* Proposed by a reviewer
* Proposed from operational analytics
* Proposed from approved agent learning
* Suggested after repeated session failures
* Suggested after evaluation disagreements
* Suggested from participant feedback
* Suggested from tool-performance evidence

The platform must not permit uncontrolled or unaudited self-modification.

Administrators control whether proposed changes:

* Are applied manually
* Require explicit approval
* May be automatically applied within defined boundaries
* Must be tested before use
* Require reviewer sign-off
* Require comparison against a prior snapshot
* Are limited to specific campaigns
* Are prohibited in stable environments

All meaningful harness changes should be recorded in an audit history.

## Harness Snapshots, Backup, and Restoration

Important harness states can be saved as immutable **Harness Snapshots**.

A snapshot captures the exact harness state at a defined point in time, including:

* Instructions
* Workflows
* Tools and permissions
* Knowledge configuration
* Rubrics
* Validation rules
* Output requirements
* Policies
* Memory controls
* Evaluation procedures
* Interaction rules
* Relevant configuration metadata

Snapshots support:

* Reproducibility
* Auditability
* Historical comparison
* Controlled rollout
* Testing
* Backup
* Restoration
* Incident investigation
* Evaluation consistency
* Regulatory review

Administrators should be able to:

* Save a harness snapshot
* Name and describe the snapshot
* Compare two snapshots
* Review individual changes
* See who created or approved a change
* Restore an earlier harness state
* Create a new harness state from an old snapshot
* Mark snapshots as approved or deprecated
* Pin an activity to a specific snapshot
* Determine which sessions used a snapshot
* Review outcomes associated with a snapshot

A campaign may use:

* The current approved harness
* A specific pinned harness snapshot
* A controlled testing version
* A restored historical configuration

Each session must record the exact harness state used, even when the campaign follows the current harness rather than a permanently pinned snapshot.

This ensures that completed sessions remain explainable and reproducible after the harness changes.

## Campaign

A **Campaign** is a structured activity configuration that coordinates an agent, a harness, participants, tasks, limits, rules, and session requirements.

Campaigns are especially useful for activities involving multiple participants, shared deadlines, common instructions, standardized workflows, or comparable outcomes.

Examples include:

* Software Engineering Final Examination — Q1 2026
* Capstone Project Evaluation
* Graduate Recruitment Interviews — August 2026
* English Speaking Practice Program
* Customer Onboarding Program
* Product Requirements Interview Series
* Leadership Coaching Cohort
* Support Quality Review
* Compliance Knowledge Assessment
* Project Retrospective Program

A campaign may configure:

* Campaign name
* Activity type
* Description
* Selected agent
* Selected harness
* Current harness or pinned snapshot
* Memory mode
* Memory override rules
* Tasks or assignments
* Participant population
* Participant groups
* Start date
* End date
* Submission deadline
* Session availability window
* Session duration
* Attempt limits
* Submission requirements
* Campaign-specific context
* Campaign-specific instructions
* Rubric and scoring rules
* Conversation protocol
* Follow-up question rules
* Tool permissions
* Voice interaction policies
* Interruption policies
* Completion conditions
* Escalation requirements
* Human-review requirements
* Result visibility
* Result release rules
* Data-retention rules
* Participant consent requirements

Campaign configuration extends or constrains the selected agent and harness without redefining the agent’s underlying identity.

Campaign-specific settings should not silently modify the reusable agent or its base harness. Changes intended to improve the agent or harness should be handled through the appropriate controlled update and snapshot process.

Although campaigns are a central coordination model for the MVP, the platform architecture should not assume that every future agent interaction must belong to a large participant campaign. Future deployments may include individually initiated sessions, embedded support interactions, API-triggered activities, or other structured execution models.

## Participant Session

A **Session** is one isolated interaction between a configured agent and:

* One participant
* An explicitly configured participant group
* An authorized reviewer
* Another permitted session role

The session is the concrete execution unit in which conversation, tools, workflow, submissions, evidence, and outcomes are recorded.

Each session may contain:

* Session identifier
* Participant identity
* Participant role
* Group membership when applicable
* Agent identity
* Campaign reference when applicable
* Harness state
* Harness snapshot reference when pinned
* Memory mode
* Memory policy
* Submitted work
* Attachments
* Text transcript
* Voice transcript
* Partial transcripts
* Agent audio playback records
* Workflow state
* Stage-transition history
* Start time
* Deadline
* Pause periods
* Completion time
* Attempt number
* Agent questions
* Participant responses
* Tool executions
* Tool results
* Interaction events
* Interruptions
* Evidence collected
* References to submitted material
* Evaluation data
* Final outcome
* Confidence values
* Human-review flags
* Release status
* Audit history

Many sessions may run concurrently under the same campaign or agent.

Their conversational state, participant information, attachments, tools, working context, evidence, and outcomes must remain isolated unless an activity explicitly permits controlled group interaction.

One participant’s session data must not be exposed to another participant or influence another participant’s experience unless this behavior is intentionally configured, authorized, and audited.

## Authoritative Session Configuration

Every session must record the exact operating configuration used during execution.

This includes:

* Agent configuration
* Harness state
* Harness snapshot when applicable
* Campaign configuration
* Memory mode
* Memory permissions
* Tool availability
* Workflow definition
* Evaluation rules
* Interaction policies
* Safety policies
* Validation rules
* Output schema

This record allows administrators and reviewers to determine exactly why the agent behaved in a particular way.

It also ensures that later changes to the agent, harness, campaign, tools, policies, or memory settings do not alter the historical meaning of a completed session.

## Workflow Model

The product may look like a chat or voice application, but it is not only a chatbot.

Text and voice are interaction surfaces. A configurable workflow determines how the activity progresses and what the agent, participant, tools, and reviewers may do at each stage.

The workflow may determine:

* The current session stage
* Which actions are permitted
* Which roles may act
* What information must be collected
* Whether a submission is required
* Whether attachments are allowed
* When the agent may ask questions
* Which questions may be asked
* Whether adaptive follow-up is permitted
* When tools may be used
* Which tools are available
* When evidence must be recorded
* When an evaluation may begin
* Whether human review is required
* When the session may be paused
* When the session may be completed
* What output must be produced
* When results may be released
* Whether memory updates may be proposed

A workflow may be defined primarily by the harness and then extended or constrained by the activity configuration.

An examination workflow may include:

1. Not started
2. Instructions presented
3. Instructions acknowledged
4. Submission required
5. Submission received
6. Submission validation
7. Agent review
8. Follow-up examination
9. Evidence consolidation
10. Evaluation
11. Human review
12. Result approved
13. Result released
14. Session archived

An interview workflow may include:

1. Candidate checked in
2. Consent confirmed
3. Interview introduction
4. Structured questions
5. Adaptive follow-up
6. Candidate questions
7. Evidence summary
8. Evaluation
9. Recruiter review
10. Decision recorded

A support workflow may include:

1. Request received
2. Identity or account verified
3. Issue classified
4. Information collected
5. Troubleshooting performed
6. Tools executed
7. Resolution proposed
8. Confirmation requested
9. Escalation or completion
10. Outcome recorded

Different activities may define different workflows while sharing the same agent, harness, session, voice, evidence, memory, and audit foundations.

## Adaptive Follow-Up Questions

The platform supports adaptive follow-up questions within configured boundaries.

The agent may use participant submissions, previous responses, tool results, workflow state, and remaining time to determine whether further questions are needed.

Follow-up behavior may be controlled by:

* Maximum question count
* Maximum follow-up depth
* Required topic coverage
* Rubric criteria
* Missing evidence
* Contradictory evidence
* Confidence thresholds
* Session time remaining
* Participant response length
* Activity fairness requirements
* Prohibited question categories
* Human-review policies

Adaptive questioning should not mean unrestricted questioning.

The harness and activity rules may require the agent to:

* Ask equivalent core questions across participants
* Ask follow-ups only when evidence is insufficient
* Avoid introducing new scoring criteria
* Record why a follow-up was asked
* Link the question to a rubric criterion
* Stop when sufficient evidence has been collected
* Avoid revealing confidential evaluation logic
* Respect accessibility accommodations
* Escalate sensitive issues to a human

## Voice Interaction Model

The MVP uses **natural, interruptible streaming conversation** rather than rigid push-to-talk interaction or unrestricted simultaneous full-duplex voice.

Speech is streamed in both directions, but one side normally owns the conversational floor at a given moment.

The system supports:

* Automatic speech-start detection
* Automatic speech-end detection
* Silence detection
* Streaming transcription
* Partial transcript generation
* Streaming agent audio
* Participant interruption of the agent
* Agent interruption of the participant
* Playback-position tracking
* Played-content tracking
* Natural floor handoff
* Backchannel recognition
* Interruption-aware conversation history
* Time-aware pacing
* Policy-controlled interruption behavior

The platform must distinguish between:

* Content generated by the agent
* Content sent for playback
* Content actually played
* Content cancelled before playback
* Content interrupted during playback
* Content likely heard by the participant

Only content actually played should be treated as heard by the participant.

This distinction is important for conversation continuity, transcript accuracy, evaluation fairness, and auditability.

## Participant Interrupting the Agent

When a participant begins meaningful speech while the agent is speaking:

1. The system detects speech activity.
2. It distinguishes meaningful interruption from noise or a brief backchannel.
3. Agent playback is paused or cancelled when appropriate.
4. The exact playback position is recorded.
5. Played content is distinguished from unplayed content.
6. The participant receives the conversational floor.
7. The participant’s speech is transcribed.
8. The participant’s speech becomes the next conversation message when complete.
9. The main agent receives structured interruption context.
10. The agent decides whether to acknowledge, resume, summarize, rephrase, or abandon the previous response.
11. The authoritative session state and event log are updated.

The system should not assume that the participant heard text that was generated but never played.

The main agent may receive information such as:

* The portion of the response that was played
* The portion that remained unplayed
* The playback timestamp
* The participant’s interruption transcript
* Whether the interruption appeared intentional
* The current workflow stage
* The remaining session time
* Relevant interaction policies

## Agent Interrupting the Participant

While the participant is speaking, the system may determine that the agent should:

* Continue listening
* Provide a backchannel
* Wait for a natural pause
* Ask for clarification
* Redirect the participant
* Interrupt the participant
* End the response under a hard policy

Possible reasons to interrupt include:

* The answer exceeds a configured duration
* The participant becomes highly repetitive
* The response moves significantly off-topic
* The required answer has already been provided
* The participant is answering a different question
* The session is approaching its time limit
* An immediate clarification is necessary
* A safety policy requires intervention
* The participant attempts a prohibited action
* The workflow requires a strict response boundary

The system should prefer a natural pause, sentence boundary, or clear transition before interrupting unless a hard policy requires immediate action.

Interruption behavior should be configurable by activity type. A coaching session may permit long reflective answers, while a timed assessment may require stricter pacing.

## Interaction Controller

A separate **Interaction Controller** manages real-time conversational behavior.

It is distinct from the main conversational agent.

The main agent owns:

* Semantic understanding
* Domain reasoning
* Task reasoning
* Tool selection
* Tool usage
* Question generation
* Follow-up reasoning
* Evidence interpretation
* Evaluation
* Decision-making
* Substantive response content

The Interaction Controller owns:

* Floor management
* Speech timing
* Silence timing
* Interruption detection
* Continue, pause, yield, or interrupt decisions
* Backchannel decisions
* Noise handling
* Natural handoff timing
* Conversation pacing
* Playback cancellation
* Interaction-policy enforcement
* Re-evaluation timing

This separation allows the main agent to focus on the meaning and purpose of the interaction while a lightweight controller responds to real-time audio and timing signals.

The Interaction Controller may receive compact information such as:

* Current speaker
* Current session stage
* Speech duration
* Silence duration
* Partial transcript
* Transcript stability
* Agent playback position
* Agent playback state
* Current question
* Current response intent
* Remaining session time
* Participant interruption history
* Interaction policies
* Voice configuration
* Workflow constraints

It may return structured actions such as:

* Continue listening
* Continue speaking
* Pause playback
* Cancel playback
* Yield to participant
* Interrupt participant
* Ignore noise
* Treat speech as a backchannel
* Wait for a sentence boundary
* Ask the main agent for substantive reasoning
* Re-evaluate after a specified interval

The Interaction Controller should use a hybrid approach:

1. Deterministic audio, timing, and playback rules
2. Lightweight behavioral classification when meaning is ambiguous
3. Main-agent reasoning only when substantive conversational understanding is required

The controller should be event-driven rather than continuously invoking a full reasoning model at very short intervals.

Future Interaction Controller responsibilities may include:

* Engagement detection
* Adaptive speaking pace
* Backchannel generation
* Conversation-style enforcement
* Moderation and escalation
* Emotional or behavioral cue detection
* Accessibility adaptations
* Session pacing
* Human handoff decisions
* Group-conversation floor management
* Dynamic latency management
* Participant-specific interaction accommodations

## Authoritative Session State

The system maintains one canonical session state and event log.

This state is the authoritative record used by:

* The main agent
* The Interaction Controller
* Workflow execution
* Tool orchestration
* Evaluation
* Human review
* Audit processes
* Result generation

Events may include:

* Session created
* Participant authenticated
* Participant entered session
* Instructions presented
* Instructions acknowledged
* Submission uploaded
* Submission validated
* Participant started speaking
* Participant stopped speaking
* Agent started speaking
* Agent stopped speaking
* Partial transcript received
* Final transcript received
* Agent playback started
* Agent playback progressed
* Agent playback paused
* Agent playback cancelled
* Interruption detected
* Floor yielded
* Agent interrupted
* Participant interrupted
* Backchannel detected
* Noise ignored
* Tool requested
* Tool executed
* Tool failed
* Evidence recorded
* Workflow stage changed
* Session paused
* Session resumed
* Time warning issued
* Time expired
* Evaluation started
* Evaluation completed
* Human review requested
* Result approved
* Result released
* Memory proposal created
* Memory proposal approved or rejected
* Harness improvement proposed

The Interaction Controller and main agent operate against this shared state rather than maintaining independent and potentially divergent conversation histories.

The state model should support append-only event recording where appropriate, with derived current state calculated from validated events.

## Submissions and Attachments

Participants may provide work or supporting material before or during a session.

Submissions may include:

* Text responses
* Documents
* Source code
* Images
* Audio
* Presentation files
* Project artifacts
* Structured forms
* Links
* Supporting evidence
* Other administrator-approved file types

Submission configuration may define:

* Required files
* Optional files
* Allowed file types
* Maximum file sizes
* Submission deadlines
* Resubmission rules
* Version limits
* Virus or safety scanning
* Extraction and parsing behavior
* Whether the agent may inspect the submission
* Whether tools may process the submission
* Whether the participant may modify it after the session starts

The system should preserve the exact submission version used by the agent and evaluation process.

Evidence and evaluation references should point to stable locations within the submitted material whenever possible.

## Tool Execution

Agents may use tools during a session when permitted by the harness, activity configuration, workflow stage, participant permissions, and safety policies.

Tools may support:

* Document retrieval
* Submission inspection
* Code execution
* Search
* Calculation
* Data lookup
* Scheduling
* External system access
* Knowledge retrieval
* Validation
* File analysis
* Communication
* Evaluation support

Each tool execution should record:

* Tool name
* Requesting agent
* Session
* Workflow stage
* Input parameters
* Permission decision
* Execution time
* Result
* Error state
* Evidence generated
* Whether the result influenced an evaluation
* Relevant policy checks

Tool availability may vary by:

* Agent
* Harness
* Campaign
* Session stage
* Participant role
* Memory mode
* Organization
* Data classification

Tool results should not automatically be treated as correct. The harness may require validation, corroboration, confidence assessment, or human review.

## Primary MVP Use Case

The first MVP focuses on **conversational assessment and examination**.

An administrator can:

1. Create an examiner agent.
2. Configure its identity, role, knowledge, skills, tools, communication style, and evaluation behavior.
3. Select stable or dynamic agent mode.
4. Configure the harness, including workflows, policies, rubrics, tools, validation rules, memory controls, and output requirements.
5. Save an initial harness snapshot.
6. Create an assessment campaign.
7. Select the agent and harness configuration.
8. Pin the campaign to a snapshot or use the current approved harness.
9. Define tasks, participant instructions, submission requirements, rubrics, deadlines, time limits, attempts, and interaction rules.
10. Add or import participants.
11. Allow each participant to submit work.
12. Validate and preserve the submitted material.
13. Create an isolated session for each participant.
14. Let the agent inspect the permitted submission.
15. Conduct a structured text or interruptible voice examination.
16. Ask adaptive follow-up questions when permitted.
17. Use approved tools where necessary.
18. Collect evidence from the submission, conversation, and tool results.
19. Produce a structured evaluation.
20. Flag uncertainty or cases requiring human review.
21. Allow reviewers to inspect the evidence, transcript, evaluation, and session configuration.
22. Approve and release the result.
23. Use approved feedback and outcomes to propose memory or harness improvements when permitted.
24. Save, compare, or restore harness snapshots as the system evolves.

The assessment workflow demonstrates the platform’s core capabilities without making assessment-specific concepts part of every agent or session.

## Evaluation Model

Evaluations are structured, evidence-backed, and auditable rather than unrestricted chat responses.

An evaluation is organized by rubric criterion or another configured decision framework.

For each criterion, the system may capture:

* Criterion identifier
* Criterion name
* Criterion description
* Score
* Maximum score
* Weight
* Evaluation summary
* Supporting evidence
* Submission references
* Conversation references
* Tool-result references
* Confidence level
* Uncertainty explanation
* Conflicting evidence
* Missing evidence
* Flags requiring human review
* Reviewer comments
* Approval status

The complete evaluation may include:

* Criterion-level scores
* Weighted score
* Overall score
* Pass, fail, or classification outcome
* Strengths
* Weaknesses
* Follow-up answer analysis
* Evidence references
* Confidence summary
* Uncertainty summary
* Final feedback
* Recommended next steps
* Review status
* Release status
* Evaluation version
* Reviewer identity
* Approval history

The evaluation must maintain an auditable connection between:

* The configured rubric
* The participant’s submission
* The conversation transcript
* Tool results
* Collected evidence
* Criterion-level reasoning
* Scores or decisions
* Final feedback

The system should support uncertainty rather than forcing false precision.

An agent may indicate that:

* Evidence is insufficient
* Evidence is contradictory
* A criterion could not be assessed
* A tool result may be unreliable
* Human judgment is required
* A response may have been affected by interruption
* The participant may not have heard part of a question
* A session failure affected the outcome

This evaluation model is a major differentiator from generic conversational AI products.

## Human Review

Human review may be required or optional depending on the activity.

Reviewers should be able to inspect:

* Participant submissions
* Session transcripts
* Voice interruption events
* Tool executions
* Evidence references
* Criterion-level evaluations
* Confidence levels
* Uncertainty flags
* Agent configuration
* Harness state
* Harness snapshot
* Memory mode
* Campaign rules
* Workflow history
* Result-release state

Reviewers may be permitted to:

* Approve an evaluation
* Adjust a score
* Add comments
* Request re-evaluation
* Mark evidence as invalid
* Resolve uncertainty
* Return a session for additional review
* Approve result release
* Propose memory updates
* Propose harness improvements
* Flag an agent or harness issue

Human changes should preserve the original agent-generated evaluation and create an auditable revision rather than silently replacing historical output.

## Harness and Memory Improvement Cycle

Approved outcomes and feedback may contribute to agent or harness improvement when permitted.

A controlled improvement cycle may include:

1. A session is completed.
2. The agent generates an evaluation or outcome.
3. A reviewer inspects the evidence.
4. The reviewer approves, corrects, or rejects the outcome.
5. The system identifies a potential agent memory or harness improvement.
6. A structured proposal is created.
7. The proposal includes supporting sessions and evidence.
8. An administrator reviews the proposal.
9. The proposal is approved, edited, rejected, or deferred.
10. An approved memory is added or updated.
11. An approved harness change is applied.
12. A new harness snapshot is saved when appropriate.
13. The updated configuration is tested.
14. Future sessions record the new state they use.

This process allows continuous improvement without uncontrolled self-modification.

## Audit and Reproducibility

The platform must make important outcomes explainable and reproducible.

For each session, administrators should be able to determine:

* Which agent was used
* Which harness state was used
* Which snapshot was used when applicable
* Which campaign rules applied
* Which memory mode was active
* Which memories were available
* Which tools were available
* Which tools were executed
* Which workflow stages occurred
* What the participant submitted
* What was said
* What audio was played
* Where interruptions occurred
* What evidence was collected
* How an evaluation was produced
* Which human changes were made
* Whether the result was released
* Whether the session contributed to memory or harness learning

Audit records should support:

* Internal quality review
* Participant appeals
* Regulatory review
* Incident investigation
* Model-behavior analysis
* Fairness review
* Configuration comparison
* Harness rollback
* Evaluation calibration
* Operational debugging

## MVP Administrative Capabilities

Administrators can:

* Create and manage agents
* Configure agent identity and behavior
* Configure roles and responsibilities
* Configure agent knowledge
* Configure skills and tools
* Configure safety and operational boundaries
* Configure evaluation behavior
* Select default memory modes
* Enable or disable memory
* Inspect and manage stored memories
* Configure harnesses
* Configure workflows and procedures
* Configure policies and validation rules
* Configure rubrics
* Configure output schemas
* Configure voice interaction
* Configure interruption behavior
* Configure Interaction Controller policies
* Save harness snapshots
* Compare harness snapshots
* Restore earlier harness states
* Review harness change history
* Approve or reject harness improvement proposals
* Create campaigns
* Select current or pinned harness configurations
* Define tasks and submission requirements
* Configure time, deadline, and attempt limits
* Configure participant instructions
* Add or import participants
* Configure participant groups
* Monitor active sessions
* Pause or terminate sessions when authorized
* Inspect submissions
* Inspect transcripts
* Inspect interaction events
* Inspect tool executions
* Review evidence
* Review evaluations
* Manage human-review workflows
* Approve and release results
* Review memory proposals
* Review harness proposals
* Inspect audit history
* Export permitted records

## MVP Participant Capabilities

Participants can:

* Access an assigned or authorized session
* Review activity instructions
* Acknowledge rules and consent requirements
* Submit required work
* Upload permitted attachments
* Start a timed session
* Communicate through text
* Participate in streaming voice conversation
* Hear streaming agent speech
* Interrupt the agent naturally
* Receive clarification
* Be redirected or interrupted according to configured policy
* Respond to adaptive follow-up questions
* View session status
* View remaining time
* Receive time warnings
* Pause when permitted
* Complete or submit the session
* View completion confirmation
* View results after release
* View feedback when permitted
* Request review or appeal when supported

## MVP Reviewer Capabilities

Reviewers can:

* Access assigned sessions
* Inspect submissions
* Inspect text and voice transcripts
* Inspect interruption and playback events
* Review collected evidence
* Review criterion-level evaluations
* Review confidence and uncertainty
* Compare agent conclusions with evidence
* Adjust or comment on evaluations when authorized
* Approve or reject results
* Request additional review
* Release results when authorized
* Propose memory updates
* Propose harness improvements
* Compare relevant harness states
* Review audit history

## Security, Privacy, and Isolation Principles

Participant sessions must remain isolated.

The system should enforce:

* Role-based access
* Participant-data isolation
* Campaign-data isolation
* Organization-level boundaries
* Tool permission controls
* File access controls
* Memory-use controls
* Audit logging
* Secure attachment handling
* Configurable retention
* Controlled export
* Reviewer authorization
* Result-visibility rules

Participant data must not be reused for agent learning unless explicitly permitted.

Dynamic memory should not create unrestricted cross-participant knowledge.

Sensitive information should remain scoped to the participant, session, campaign, or organization according to policy.

The system should preserve a clear distinction between:

* Session context
* Campaign context
* Agent memory
* Harness knowledge
* Organization knowledge
* Public knowledge
* Reviewer feedback

## Deferred Features

The MVP does not initially require:

* Unrestricted simultaneous full-duplex voice conversation
* Video conversation
* Automated human proctoring
* Multi-agent collaboration inside one session
* Multi-agent debate or delegation
* Visual workflow builders
* Public agent marketplaces
* Public tool marketplaces
* Advanced cheating detection
* Biometric identity verification
* Unrestricted cross-campaign participant memory
* Unrestricted cross-organization memory
* Fully autonomous agent self-modification
* Uncontrolled harness modification
* Automatic application of high-risk learning proposals
* Complex billing systems
* Advanced organization management
* Large-scale workforce scheduling
* Fully autonomous result release
* General-purpose collaborative workspaces

These features may be introduced later without changing the core separation between agents, harnesses, activities, sessions, memory controls, authoritative state, evidence, and audit history.

## Product Positioning

The architecture supports a flexible conversational AI agent platform.

The first product experience is positioned as:

> **An AI assessment and examination platform built on a reusable, memory-controlled conversational-agent foundation.**

The broader platform allows organizations to create agents that can operate consistently or improve over time, deploy them into structured activities, conduct isolated text or interruptible voice sessions, use tools, follow controlled workflows, collect evidence, and produce auditable outcomes.

The platform differentiates itself through:

* Reusable agent identities
* Stable and dynamic agent modes
* Administratively controlled memory
* Mutable but governed harnesses
* Harness snapshots and restoration
* Structured workflows
* Natural interruptible voice
* Authoritative session state
* Participant isolation
* Evidence-backed evaluations
* Human-review support
* Reproducible outcomes
* Detailed audit history

## Product Statement

A platform for creating reusable conversational AI agents with configurable identities, roles, knowledge, tools, memory modes, evaluation behavior, and operating boundaries, then running those agents through governed harnesses in structured, multi-session activities involving individual participants or participant groups.

The platform supports text and natural interruptible voice interaction, participant submissions, adaptive follow-up questions, tool use, controlled workflows, evidence collection, structured evaluations, human review, memory management, harness improvement, snapshots, backup, restoration, and complete auditability.

For the first MVP:

> Create an AI examiner, select stable or dynamic agent behavior, configure a controlled assessment harness, save a reproducible harness snapshot, define an assessment activity, allow participants to submit work and complete natural text or interruptible voice examinations, collect evidence, and generate structured evaluations against a predefined rubric.

The same foundation can later power interviews, coaching, reviews, requirements discovery, onboarding, customer support, and other structured conversational activities without redefining the platform around any single campaign type or use case.
