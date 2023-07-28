# Historia
Historia is a special kind of language to express branching stories for games in the spirit of "Life is Strange" or "Detroit: Become Human". From the imperative code the Historia compiler generates a state machine in the form of a C# class. For more info on the language and the compiler look into the `docs` folder. Examples will later be added in the `examples` folder.

## Roadmap
### MVP
- [x] Linear states (`output`)
- [x] Branching states (`switch`)
- [x] Record types
- [x] Saving previous choices (`outcome`, named `switch`)
- [x] Branching on previous choices (`branchon`)
- [ ] `union` types
- [ ] `enum` types
- [ ] Scenes other than `main` scene
- [ ] Global `outcome`s and `switch`es carrying out global `outcome`s
- [ ] Roslyn code generator
- [ ] Splitting Historia code across multiple files

### Framework features
- [ ] Visual Studio language extension
- [ ] Immutable story classes
- [ ] Generating flow graphs
- [ ] Dynamic compilation
- [ ] Analysis features

### More 
- [ ] Cumulative outcomes/variables
- [ ] Inputs
