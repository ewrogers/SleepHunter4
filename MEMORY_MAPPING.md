# Client Memory Mapping Audit

## Scope

This audit compares SleepHunter's unified client layout and immutable snapshot
model with the `darkages-741-re` runtime documentation, starting with
[Runtime state walking](https://github.com/ewrogers/darkages-741-re/blob/main/docs/appendix/runtime/state-walking.md).

The documented addresses apply to `Darkages.exe` 7.4.1.0 with SHA-256:

```text
054A5D6ADC56099C6BFD9D2A58675AFF62DC788B63209A3D906492F5B89E96C6
```

SleepHunter uses one `ClientLayout.xml` for every attached process. Each client
runtime resolves and captures its own process state into a separate immutable
`ClientSnapshot`. The mapping is unified across processes, but snapshots and
generation checks are never shared between clients.

## Root Reconciliation

The reference documents two adjusted interface roots. SleepHunter's negative
or reduced offsets account for those adjustments:

| Root | Stored value | SleepHunter adjustment |
| --- | --- | --- |
| `0x0073D964` | `WorldPane + 0x2EC` | World fields use negative offsets; session and object-list fields recover their documented child pointers |
| `0x0082B768` | `GUIBackPane + 0x190` | GUI child offsets are reduced by `0x190` before dereferencing |
| `0x006FC914` | complete `EquipPane *` | Equipment fields use their direct offsets |

The supported executable's preferred image addresses remain in
`data/ClientLayout.xml`. Client patching independently verifies the executable
hash before applying byte patches.

## Coverage

| Requested state | Snapshot coverage | Primary source |
| --- | --- | --- |
| Character | Complete for documented durable fields | `WorldUserFunc`, status pane, extra-status pane, static bounded name buffer |
| Inventory | Slot, identity name, display name, sprite, dye, quantity, stackability, current durability, maximum durability | Compact session inventory enriched from item panes |
| Skills | Slot, name, icon, levels, action delay, cooldown progress, cooldown timestamps, visual cooldown flag, application metadata | Skill pane with compact-session fallback |
| Spells | Slot, name, icon, levels, prompt, argument type, cast lines, action delay, application metadata | Spell pane with compact-session fallback |
| Equipment | All 18 slots with name, sprite, dye, current durability, maximum durability | Direct `EquipPane` singleton |
| Group | Cached member names and starred bytes, up to 64 entries | `WorldUserFunc + 0x0004`, count at `+0x1044` |
| Map | ID, width, height, camera coordinates, flags, weather, transfer state, and best-effort name | `WorldPane`, plus the existing GUI name source |
| World entities | ID, tile coordinates, broad type, draw layer, broad category, collision, direction, creature type, and available appearance | Bounded `WorldObjectList` tree walk with exact RTTI names |
| Active spell effects | Slot, icon, server duration stage | `SpelledViewPane` parallel icon and stage arrays |

The richer character snapshot includes class, level, ability level, user state,
privilege level, character ID, gold, total experience, attributes, stat points,
progression totals, weight, armor class, damage, hit, elements, magic resistance,
action state, and self-look metadata flags.

## Collection and Field Rules

Inventory first captures the compact 60-slot session array. For each present
non-gold slot, it attempts to enrich the immutable item from the matching pane
pointer. Pane identity must agree on slot and sprite, and the pointer table must
remain unchanged through the read. If pane state changes or is unavailable, the
compact item remains usable.

Equipment reads one bounded `0x9C8`-byte pane snapshot and copies every present
slot. The documented in-memory durability order is maximum followed by current.
Both the Interop parser and the still-active application projection use this
order.

Skill and spell panes preserve client-owned values that the compact session
arrays omit. The compact arrays remain compatibility fallbacks when a pane
table changes during capture.

The group roster is the most recent `SSelfLook` cache, not a visible-player
list. A zero count is an empty roster. Names are unique in the published
snapshot, and the raw starred byte is retained without assigning it an
unverified social meaning.

## World Entities

The entity reader captures the list owner, head sentinel, and tree root, then
performs a bounded in-order walk with a 512-node limit. It aborts without
publishing the collection when it encounters a null object, repeated node,
identity mismatch, invalid insertion state, or changed tree identity.

Exact RTTI classes provide the broad classification:

| Client class or protocol value | Runtime type |
| --- | --- |
| `WorldObject_Item` | `GroundItem` |
| `WorldObject_Human`, `WorldObject_User` | `Player` |
| `WorldObject_Monster`, creature type `2` | `NonPlayerCharacter` |
| `WorldObject_Monster`, creature type `4` | `Player` |
| Other `WorldObject_Monster` values | `Monster` |

Ground items retain numeric sprite and dye values. Human objects retain a
structured appearance record, whose body sprite is exposed as the entity
sprite while the renderer uses that human appearance.

Monster and NPC objects resolve their numeric selector into a
`MonsterObjectImageSession` and monster resource, then discard the documented
scalar selector. Their `Sprite` remains null rather than deriving an unverified
number. The snapshot still copies the image-session, object-resource, and
image-session-resource addresses as opaque, generation-scoped identity values.
Consumers can compare those identities within snapshots but must not
dereference them later. They identify the resolved appearance even though they
do not recover the discarded numeric selector.

A human or local-user object can use a monster image session as a disguise. It
remains a `Player`, reports `AppearanceKind.Monster`, leaves `Sprite` null, and
retains its human appearance record separately. This prevents class-only
classification from presenting an inactive human body sprite as the current
rendered appearance.

Capturing the exact monster selector requires a guarded client hook at the
appearance-application boundary. The reference identifies
`render_monster_apply_appearance` at static `0x005E0370`, RVA `0x001E0370`, as
receiving the object and untagged selector. SleepHunter does not patch this
function yet. Its calling convention, registers, overwrite signature, cache
lifetime, and cleanup behavior must be verified before implementing that
high-risk patch.

The tree represents entities currently known to the client. It is not a claim
that every entity was drawn in the most recent frame.

## Active Spell Effects

The effect pane owns ten signed 16-bit icon entries followed by ten signed
duration-stage bytes. An icon of `-1` or stage `0` is absent. Published stages
are:

1. Blue
2. Green
3. Yellow
4. Orange
5. Red
6. White

The client does not retain seconds remaining, an expiry timestamp, or a
last-update time for these effects. The immutable model therefore exposes
`DurationStage`, not a misleading `TimeSpan`. Exact expiry would require
packet-time tracking in addition to memory capture.

## Coherence and Known Limits

The reference's strongest snapshot guarantee requires walking client-owned
state on the game thread. SleepHunter is an external read-only process observer,
so it cannot make the entire remote walk transactionally atomic. It compensates
with bounded reads, root and pointer-table generation checks, character and map
ownership checks, read-compare validation for mapped collection blocks, tree
identity checks, and rejection of partial or incoherent captures.

The map ID, dimensions, coordinates, flags, weather, and transfer state are
durable `WorldPane` fields. The client parses the map name from `SMapSize` but
does not retain it in an authoritative `WorldPane` field. SleepHunter currently
uses its executable-verified GUI string pointer and requires the name and map ID
to stabilize before publishing a transition. Packet-tracking the accepted
`SMapSize` name remains the correct future replacement for that best-effort
source.

Static absolute addresses assume the supported executable's preferred image
layout. Supporting a relocated custom executable requires an explicit
module-base relocation layer and a separately verified layout, not silent reuse
of the current addresses.

## Automated Evidence

Deterministic tests cover:

- Typed scalar and full-snapshot capture from a synthetic process image.
- Compact and pane-enriched inventory, skill, and spell records.
- All-slot equipment parsing and documented durability ordering.
- Group bounds, names, and starred state.
- Active effect icon and stage parsing.
- World tree traversal, RTTI normalization, entity classification, sprites,
  coordinates, appearance, and generation-change rejection.
- Mapping roots, adjusted offsets, record sizes, and capacities.

These tests do not require a live client. Live smoke testing should still
confirm the supported executable's mappings and capture timing under normal
map traffic.
