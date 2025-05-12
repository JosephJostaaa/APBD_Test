using System.Data.Common;
using APBD_Test1.Excpetion;
using APBD_Test1.Models;
using APBD_Test1.Services;
using Microsoft.Data.SqlClient;

namespace APBD_Test1.Repositories;

public class DbService : IDbService
{
    
    private readonly string _connectionString;

    public DbService(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("Default");
    }

    public async Task<Visit> GetVisitByIdAsync(int visitId, CancellationToken ct)
    {
        await using SqlConnection connection = new SqlConnection(_connectionString);
        await using SqlCommand command = new SqlCommand();

        command.Connection = connection;
        await connection.OpenAsync(ct);

        command.CommandText = @"SELECT * FROM Visit v JOIN s30209.Client C on C.client_id = v.client_id
                                JOIN s30209.Mechanic M on v.mechanic_id = M.mechanic_id
                                JOIN s30209.Visit_Service VS on v.visit_id = VS.visit_id
                                JOIN s30209.Service S on VS.service_id = S.service_id
                                WHERE v.visit_id = @VisitId";
        
        command.Parameters.AddWithValue("@VisitId", visitId);
        

        await using SqlDataReader reader = await command.ExecuteReaderAsync(ct);

        Visit visit = null;
        
        while (await reader.ReadAsync(ct))
        {
            if (visit == null)
            {
                visit = new()
                {
                    VisitId = reader.GetInt32(reader.GetOrdinal("Visit_Id")),
                    VisitDate = reader.GetDateTime(reader.GetOrdinal("Date")),
                    Client = new()
                    {
                        DateOfBirth = reader.GetDateTime(reader.GetOrdinal("Date_Of_Birth")),
                        FirstName = reader.GetString(reader.GetOrdinal("First_Name")),
                        LastName = reader.GetString(reader.GetOrdinal("Last_Name")),
                    },

                    Mechanic = new()
                    {
                        MechanicId = reader.GetInt32(reader.GetOrdinal("Mechanic_Id")),
                        LicenceNumber = reader.GetString(reader.GetOrdinal("Licence_Number"))
                    },

                    VisitServices = new List<Service>
                    {
                        new Service()
                        {
                            ServiceName = reader.GetString(reader.GetOrdinal("Name")),
                            ServiceFee = reader.GetDecimal(reader.GetOrdinal("Service_Fee")),
                        }
                    }
                };
            }
            else
            {
                visit.VisitServices.Add(new Service()
                {
                    ServiceName = reader.GetString(reader.GetOrdinal("Name")),
                    ServiceFee = reader.GetDecimal(reader.GetOrdinal("Service_Fee"))
                });
            }
        }
        return visit;
    }


    public async Task<int> AddVisitAsync(VisitRequestDto visitRequestDto, CancellationToken ct)
    {

        if (await VisisExistByIdAsync(visitRequestDto.VisitId, ct))
        {
            throw new NotFoundException("Visit not found");
        }

        if (await ClientExistsById(visitRequestDto.ClientId, ct))
        {
            throw new NotFoundException("Client not found");
        }

        if (await MechanicExistsByLicenceNumber(visitRequestDto.MechanicLicenceNumber, ct))
        {
            throw new NotFoundException("Mechanic not found");
        }

        for (int i = 0; i < visitRequestDto.ServiceRequests.Count; i++)
        {
            if (await ServiceExistsByName(visitRequestDto.ServiceRequests[i].ServiceName, ct))
            {
                throw new NotFoundException("Service not found");
            }   
        }
        string query = @"INSERT INTO VISIT (VISIT_ID, CLIENT_ID, MECHANIC_ID, DATE) 
                        VALUES (@VisitId, @ClientId, @MechanicId, @Date)";
        
        int mechanic_id = await FindMechanicIdByLicence(visitRequestDto.MechanicLicenceNumber, ct);
        
        
        await using SqlConnection connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        
        
        
        DbTransaction transaction = await connection.BeginTransactionAsync();

        try
        {
            await using SqlCommand command = new SqlCommand();
            command.Transaction = transaction as SqlTransaction;

            command.Connection = connection;

            command.CommandText = query;
            command.Parameters.AddWithValue("@VisitId", visitRequestDto.VisitId);
            command.Parameters.AddWithValue("@ClientId", visitRequestDto.ClientId);
            command.Parameters.AddWithValue("@MechanicId", mechanic_id);
            command.Parameters.AddWithValue("@Date", DateTime.Now);

            for (int i = 0; i < visitRequestDto.ServiceRequests.Count; i++)
            {
                command.Parameters.Clear();

                command.CommandText = @"INSERT INTO Visit_Service (visit_id, service_id,  service_fee)
                                    VALUES (@VisitId, @ServiceId, @ServiceFee)";

                command.Parameters.AddWithValue("@VisitId", visitRequestDto.VisitId);
                command.Parameters.AddWithValue("@ServiceId", FindServiceIdByName(visitRequestDto.ServiceRequests[i].ServiceName, ct));
                command.Parameters.AddWithValue("@ServiceFee", visitRequestDto.ServiceRequests[i].ServiceFee);
            }

            int rowsAffected = await command.ExecuteNonQueryAsync(ct);
            
            await transaction.CommitAsync(ct);
            return rowsAffected;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<int> FindServiceIdByName(string name, CancellationToken ct)
    {
        await using SqlConnection connection = new SqlConnection(_connectionString);
        await using SqlCommand command = new SqlCommand();

        command.Connection = connection;
        await connection.OpenAsync(ct);

        command.CommandText = @"SELECT service_id FROM Service WHERE Name = @Name";
        
        command.Parameters.AddWithValue("@Name", name);
        

        await using SqlDataReader reader = await command.ExecuteReaderAsync(ct);

        int id = 0;
        
        while (await reader.ReadAsync(ct))
        {
            id = reader.GetInt32(reader.GetOrdinal("service_id"));
        }
        return id;
    }
    private async Task<int> FindMechanicIdByLicence(string licenceNumber, CancellationToken ct)
    {
        await using SqlConnection connection = new SqlConnection(_connectionString);
        await using SqlCommand command = new SqlCommand();

        command.Connection = connection;
        await connection.OpenAsync(ct);

        command.CommandText = @"SELECT mechanic_id FROM Mechanic WHERE Licence_Number = @LicenceNumber";
        
        command.Parameters.AddWithValue("@LicenceNumber", licenceNumber);
        

        await using SqlDataReader reader = await command.ExecuteReaderAsync(ct);

        int id = 0;
        
        while (await reader.ReadAsync(ct))
        {
            id = reader.GetInt32(reader.GetOrdinal("mechanic_id"));
        }
        return id;
    }
    public async Task<bool> ServiceExistsByName(string ServiceName, CancellationToken ct)
    {
        string query = @"SELECT COUNT(*) FROM Service WHERE Name = @ServiceName";
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            using (SqlCommand command = new SqlCommand())
            {
                command.Connection = conn;
                command.CommandText = query;
                
                command.Parameters.AddWithValue("@ServiceName", ServiceName);
                await conn.OpenAsync(ct);
                var result = await command.ExecuteScalarAsync(ct);
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result) > 0;
                }
                
                return false;
                
            }
        }
    }
    
    public async Task<bool> MechanicExistsByLicenceNumber(string LicenceNumber, CancellationToken ct)
    {
        string query = @"SELECT COUNT(*) FROM Mechanic WHERE licence_umber = @LicenceNumber";
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            using (SqlCommand command = new SqlCommand())
            {
                command.Connection = conn;
                command.CommandText = query;
                
                command.Parameters.AddWithValue("@LicenceNumber", LicenceNumber);
                await conn.OpenAsync(ct);
                var result = await command.ExecuteScalarAsync(ct);
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result) > 0;
                }
                
                return false;
                
            }
        }
    }
    
    public async Task<bool> ClientExistsById(int clientId, CancellationToken ct)
    {
        string query = @"SELECT COUNT(*) FROM Client WHERE customer_id = @IdClient";
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            using (SqlCommand command = new SqlCommand())
            {
                command.Connection = conn;
                command.CommandText = query;
                
                command.Parameters.AddWithValue("@IdClient", clientId);
                await conn.OpenAsync(ct);
                var result = await command.ExecuteScalarAsync(ct);
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result) > 0;
                }
                
                return false;
                
            }
        }
    }


    public async Task<bool> VisisExistByIdAsync(int visitId, CancellationToken ct)
    {
        string query = @"SELECT COUNT(*) FROM Visit WHERE visit_id = @IdVisit";
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            using (SqlCommand command = new SqlCommand())
            {
                command.Connection = conn;
                command.CommandText = query;
                
                command.Parameters.AddWithValue("@IdVisit", visitId);
                await conn.OpenAsync(ct);
                var result = await command.ExecuteScalarAsync(ct);
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result) > 0;
                }
                
                return false;
                
            }
        }
    }
}