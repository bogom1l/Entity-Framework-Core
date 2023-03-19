using AutoMapper;
using AutoMapper.QueryableExtensions;
using CarDealer.Data;
using CarDealer.DTOs.Export;
using CarDealer.DTOs.Import;
using CarDealer.Models;
using CarDealer.Models.Utilities;
using System.Text;
using System.Xml.Serialization;

namespace CarDealer
{
    public class StartUp
    {
        public static void Main()
        {
            //Analogue to JsonConvert <-> XmlSerializer

            //Import
            /*
            CarDealerContext context = new CarDealerContext();
            string inputXml = File.ReadAllText("../../../Datasets/sales.xml");

            string result = ImportSales(context, inputXml);
            Console.WriteLine(result);
            */

            //Export
            using CarDealerContext context = new CarDealerContext();

            string result = GetCarsWithDistance(context);
            Console.WriteLine(result);
        }

        private static IMapper InitializeAutoMapper()
            => new Mapper(new MapperConfiguration(cfg => { cfg.AddProfile<CarDealerProfile>(); }));

        //Problem 09
        public static string ImportSuppliers(CarDealerContext context, string inputXml)
        {
            IMapper mapper = InitializeAutoMapper();

            //Serialize + Deserialize
            XmlHelper xmlHelper = new XmlHelper();
            ImportSupplierDto[] supplierDtos = xmlHelper.Deserialize<ImportSupplierDto[]>(inputXml, "Suppliers");

            ICollection<Supplier> validSuppliers = new HashSet<Supplier>();
            foreach (ImportSupplierDto supplierDto in supplierDtos)
            {
                if (string.IsNullOrEmpty(supplierDto.Name))
                {
                    continue;
                }

                //Manual mapping
                /*Supplier supplier = new Supplier()
                {
                    Name = supplierDto.Name,
                    IsImporter = supplierDto.IsImporter
                };
                */

                Supplier supplier = mapper.Map<Supplier>(supplierDto);
                validSuppliers.Add(supplier);
            }

            context.Suppliers.AddRange(validSuppliers);
            context.SaveChanges();

            return $"Successfully imported {validSuppliers.Count}";
        }

        //Problem 10
        public static string ImportParts(CarDealerContext context, string inputXml)
        {
            IMapper mapper = InitializeAutoMapper();

            XmlHelper xmlHelper = new XmlHelper();
            ImportPartDto[] partsDtos = xmlHelper.Deserialize<ImportPartDto[]>(inputXml, "Parts");
            ICollection<Part> validParts = new HashSet<Part>();

            foreach (ImportPartDto partDto in partsDtos)
            {
                if (string.IsNullOrEmpty(partDto.Name))
                {
                    continue;
                }
                if (!partDto.SupplierId.HasValue ||
                    !context.Suppliers.Any(s => s.Id == partDto.SupplierId))
                {
                    continue;
                }

                Part part = mapper.Map<Part>(partDto);
                validParts.Add(part);
            }

            context.Parts.AddRange(validParts);
            context.SaveChanges();

            return $"Successfully imported {validParts.Count}";
        }

        //Problem 11
        public static string ImportCars(CarDealerContext context, string inputXml)
        {
            IMapper mapper = InitializeAutoMapper();
            XmlHelper xmlHelper = new XmlHelper();
            ImportCarDto[] carDtos = xmlHelper.Deserialize<ImportCarDto[]>(inputXml, "Cars");

            ICollection<Car> validCars = new HashSet<Car>();
            foreach (ImportCarDto carDto in carDtos)
            {
                if (string.IsNullOrEmpty(carDto.Make) ||
                    string.IsNullOrEmpty(carDto.Model))
                {
                    continue;
                }

                Car car = mapper.Map<Car>(carDto);

                foreach (var partDto in carDto.Parts.DistinctBy(p => p.PartId))
                {
                    if (!context.Parts.Any(p => p.Id == partDto.PartId))
                    {
                        continue;
                    }

                    PartCar carPart = new PartCar()
                    {
                        PartId = partDto.PartId
                    };

                    car.PartsCars.Add(carPart);
                }

                validCars.Add(car);
            }

            context.Cars.AddRange(validCars);
            context.SaveChanges();

            return $"Successfully imported {validCars.Count}";
        }

        //Problem 12
        public static string ImportCustomers(CarDealerContext context, string inputXml)
        {
            IMapper mapper = InitializeAutoMapper();
            XmlHelper xmlHelper = new XmlHelper();

            ImportCustomerDto[] customerDtos = xmlHelper.Deserialize<ImportCustomerDto[]>(inputXml, "Customers");

            ICollection<Customer> validCustomers = new HashSet<Customer>();
            foreach (ImportCustomerDto customerDto in customerDtos)
            {
                if (string.IsNullOrEmpty(customerDto.Name) ||
                    string.IsNullOrEmpty(customerDto.BirthDate))
                {
                    continue;
                }

                Customer customer = mapper.Map<Customer>(customerDto);
                validCustomers.Add(customer);
            }

            context.Customers.AddRange(validCustomers);
            context.SaveChanges();

            return $"Successfully imported {validCustomers.Count}";
        }

        //Problem 13
        public static string ImportSales(CarDealerContext context, string inputXml)
        {
            IMapper mapper = InitializeAutoMapper();
            XmlHelper xmlHelper = new XmlHelper();

            ImportSaleDto[] saleDtos = xmlHelper.Deserialize<ImportSaleDto[]>(inputXml, "Sales");

            ICollection<Sale> validSales = new HashSet<Sale>();

            foreach (var saleDto in saleDtos)
            {
                if (!saleDto.CarId.HasValue ||
                    !context.Cars.Any(c => c.Id == saleDto.CarId.Value))
                {
                    continue;
                }

                Sale sale = mapper.Map<Sale>(saleDto);
                validSales.Add(sale);
            }

            context.Sales.AddRange(validSales);
            context.SaveChanges();

            return $"Successfully imported {validSales.Count}";
        }

        //Problem 14 - raboti v judge, ama ne i pri men zarad Travelled
        public static string GetCarsWithDistance(CarDealerContext context)
        {
            IMapper mapper = InitializeAutoMapper();
            XmlHelper xmlHelper = new XmlHelper();

            ExportCarDto[] cars = context.Cars
                .Where(c => c.TraveledDistance > 2000000)
                .OrderBy(c => c.Make)
                .ThenBy(c => c.Model)
                .Take(10)
                .ProjectTo<ExportCarDto>(mapper.ConfigurationProvider)
                .ToArray();

            return xmlHelper.Serialize<ExportCarDto[]>(cars, "cars");
        }

        //Problem 15





    }
}