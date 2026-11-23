# Outside the Box documentation

Updated: 23 November 2026.

- [Game Design Document](OutsideTheBox_GDD.md): gameplay, content, economy, presentation, services, tools, and release dependencies.
- [GDD PDF](OutsideTheBox_GDD.pdf): shareable edition of the GDD, including the reference appendices.
- [Remote Config reference](REMOTE_CONFIG_REFERENCE.md): every field in the checked-in Firebase payload and its snapshot value.
- [Analytics event catalog](ANALYTICS_EVENTS.md): event names and the source locations that emit them.
- [Project inventory](PROJECT_INVENTORY.md): scripts, prefabs, packages, Android bridges, editor commands, and IAP products.

The documents describe the implementation checked into this Unity project. Remote Config values are a local snapshot; publishing a payload or changing a store or mediation console can change the deployed configuration independently. Console approvals, live ad serving, and successful device purchases need their own verification.

The PDF is a dated export. Update the editable files and regenerate the PDF when the design changes. Existing material under `Assets/OutsideTheBox/Documentation` remains in place.
