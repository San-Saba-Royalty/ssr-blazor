using SSRBusiness.BusinessClasses;
using SSRBusiness.Entities;

namespace SSRBlazor.Services;

public class CadDataService
{
    private readonly CadDataRepository _repository;

    public CadDataService(CadDataRepository repository)
    {
        _repository = repository;
    }

    public List<CadData> Search(CadData crit, string displayName, string tableName, string connectionString)
    {
        return _repository.Search(crit, displayName, tableName, connectionString);
    }
}
