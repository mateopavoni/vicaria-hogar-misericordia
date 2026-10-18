using Xunit;

// cada clase de test levanta su propio SQL Server en Docker; en paralelo eran hasta 16
// contenedores a la vez y tumbaban el engine (WSL con tope de RAM), colgando la suite
[assembly: CollectionBehavior(DisableTestParallelization = true)]
