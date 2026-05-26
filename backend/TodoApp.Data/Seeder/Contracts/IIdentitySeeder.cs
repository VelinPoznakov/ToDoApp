namespace TodoApp.Data.Seeder.Contracts
{
    public interface IIdentitySeeder
    {
        Task RoleSeeder();
        Task AdminSeeder();
    }
}
