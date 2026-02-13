# Fluid Sequence Module (Yi.Framework.FluidSequence)

## Phase 1: Planning & Setup
- [x] Create module via `yi-abp new FluidSequence` CLI tool (or manual structure creation)
- [x] **Organization**: Flatten directory structure (move `module/FluidSequence/FluidSequence` up)
- [x] Add all 5 projects to `Yi.Abp.sln`
  - `FluidSequence.Domain.Shared`
  - `FluidSequence.Domain`
  - `FluidSequence.Application.Contracts`
  - `FluidSequence.Application`
  - `FluidSequence.SqlSugarCore`
- [x] **Config**: Configure `FluidSequenceDbContext` (avoid naming conflict with `YiDbContext`)
- [x] **Dependency Injection**: Add module dependencies (Application, SqlSugarCore)

## Phase 2: Domain.Shared Layer
- [x] **Enum**: `SequenceResetType` (None, Daily, Monthly, Yearly, Weekly, Quarterly, FiscalYearly)
- [x] **Constants/Consts**: `FluidSequenceConsts`
- [ ] **Localization**: Add module-specific localization resources

## Phase 3: Domain Layer (Core Engine)
- [x] **Entity**: `SysSequenceRule` (SugarTable `sys_sequence_rule`, optimistic concurrency `Version`, JSON `ExtensionProps`)
  - [x] Implement `TryReset` domain logic (handling FiscalYearly etc.)
  - [x] Implement `NextValue` domain logic
- [x] **Repository**: `ISequenceRuleRepository` (inherits `ISqlSugarRepository<SysSequenceRule, long>`)
- [x] **Strategies (Pattern)**:
  - [x] `IPlaceholderStrategy` Base Interface
  - [x] `TimeStrategy` (yyyy, MM, dd, mm, ss, QQ, ww, FY)
  - [x] `RandomStrategy` (RAND:NUM, RAND:CHAR, RAND:SAFE, RAND:MIX - exclude confusing chars)
  - [x] `ContextStrategy` (UserCode, DeptCode, TenantCode, Param:Key)
  - [x] `SequenceStrategy` (SEQ, SEQ36 - Base36 conversion, CheckDigit/Luhn)
- [x] **Domain Service**: `SequenceDomainService`
  - [x] Logic for `GenerateNextAsync(ruleCode, context)`
  - [x] Regex parsing & Strategy integration
  - [x] Optimistic Concurrency Control (Retry mechanism)
  - [x] Preview logic (in-memory only, no DB update)
- [x] **Registry**: `PlaceholderRegistry` (Code-first metadata for generic placeholders)

## Phase 4: Application.Contracts Layer
- [x] **DTOs**:
  - [x] `SequenceRuleDto` (Response)
  - [x] `SequenceRuleGetListInput` (Search by Name/Code)
  - [x] `CreateSequenceRuleInput` / `UpdateSequenceRuleInput`
  - [x] `PlaceholderMetaDto` (UI Metadata)
- [x] **Service Interfaces**:
  - [x] `ISequenceRuleAppService`

## Phase 5: Application Layer (Business Logic)
- [x] **Service Implementation**: `SequenceRuleAppService`
  - [x] CRUD via `YiCrudAppService<SysSequenceRule, SequenceRuleDto, ...>`
  - [x] `TestGenerateAsync` (Preview API)
  - [x] `GetPlaceholdersAsync` (Metadata API)
  - [x] `[Authorize]` attributes for permission control
- [ ] **Caching**:
  - [ ] Cache Rule configurations (Template, Step, ResetType) to Redis
  - [ ] Implement `IDistributedCache` usage based on `appsettings.json` (Redis vs Memory)

## Phase 6: Storage & Infrastructure (SqlSugarCore)
- [x] **DbContext**: `FluidSequenceDbContext` setup
- [x] **Repository Implementation**: `SequenceRuleRepository`

## Phase 7: Web/API Integration
- [x] **Controller Registration**: `ConventionalControllers.Create` in WebModule
- [x] **Configuration**: Add `FluidSequence` section to `appsettings.json` (CacheProvider, MaxRetryCount)
- [x] **Dependency Injection**: Register Strategies and Domain Services

## Phase 8: Verification & Testing
- [ ] **Unit Tests**:
  - [ ] Concurrency Test (20+ threads) -> No duplicates
  - [ ] Reset Logic Test (Cross-day, Cross-year, Cross-Quarter)
  - [ ] Strategy Logic Test (Random generation, Base36)
- [ ] **Integration**:
  - [ ] Swagger API verification
  - [ ] Frontend Metadata API check
- [ ] **Documentation**:
  - [ ] Generate detailed `User Guide` (as per Phase 5 of docs)
  - [ ] Generate `Functional Specification`
- [ ] **Code Review**: Prepare for final review
