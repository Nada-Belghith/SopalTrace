using SopalTrace.Domain.Interfaces;

namespace SopalTrace.Domain.Entities;

public partial class PlanNonConformiteEntete : IPlanEntete { }

public partial class PlanEchantillonnageEntete : IPlanEntete { }

public partial class ModeleFabricationEntete : IPlanEntete { }

public partial class PlanProduitFiniEntete : IPlanEntete { }

public partial class PlanFabricationEntete : IPlanEntete { }

public partial class PlanAssemblageEntete : IPlanEntete { }
