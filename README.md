# Historia
Historia is a special kind of language to express branching stories for games in the spirit of "Life is Strange" or "Detroit: Become Human". From the imperative code the Historia compiler generates a state machine in the form of a C# class. For more info on the language and the compiler look into the `docs` folder. Examples will later be added in the `examples` folder.

## Roadmap
### MVP
- [x] Linear states (`output`)
- [x] Branching states (`switch`)
- [x] Record types
- [x] Saving previous choices (`outcome`, named `switch`)
- [x] `spectrum` outcomes
- [x] Branching on previous choices (`branchon`)
- [x] `union` types
- [x] `enum` types
- [x] Scenes other than `main` scene
- [x] Global `outcome`s
- [ ] Checkpoints
- [ ] Sets
- [ ] Splitting Historia code across multiple files

### Framework features
- [ ] Visual Studio language extension
- [x] Immutable story classes
- [x] Generating flow graphs
- [ ] Dynamic compilation
- [ ] Analysis features

### Quality of Life
- [ ] `union`s with shared property getting that property
- [ ] `switch`es carrying out global `outcome`s
- [ ] `branchon` with more than one outcome and more than one option per case

### More
- [ ] Finish speccing
