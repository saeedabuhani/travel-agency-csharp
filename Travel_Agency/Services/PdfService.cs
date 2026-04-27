// ===================================================
// Services/PdfService.cs - Using QuestPDF (Free)
// ===================================================

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Travel_Agency.Models;

namespace Travel_Agency.Services
{
    public static class PdfService
    {
        static PdfService()
        {
            // ✅ Set QuestPDF license type (Community is free!)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public static byte[] GenerateItinerary(Booking booking, TravelPackage package, AppUser user)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    // ✅ HEADER
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("✈️ TRAVEL ITINERARY")
                                    .FontSize(28)
                                    .Bold()
                                    .FontColor(Colors.Blue.Darken2);
                                
                                c.Item().Text("Your Adventure Awaits!")
                                    .FontSize(12)
                                    .FontColor(Colors.Grey.Darken1);
                            });

                            row.ConstantItem(100).Column(c =>
                            {
                                c.Item().Text($"Booking #{booking.Id}")
                                    .FontSize(10)
                                    .FontColor(Colors.Grey.Medium);
                                
                                c.Item().Text(DateTime.Now.ToString("dd MMM yyyy"))
                                    .FontSize(9)
                                    .FontColor(Colors.Grey.Medium);
                            });
                        });

                        col.Item().PaddingVertical(10).LineHorizontal(2).LineColor(Colors.Blue.Darken2);
                    });

                    // ✅ CONTENT
                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        // === DESTINATION HIGHLIGHT ===
                        col.Item().Background(Colors.Blue.Lighten4).Padding(20).Column(dest =>
                        {
                            dest.Item().Text($"📍 {package.Destination}")
                                .FontSize(24)
                                .Bold()
                                .FontColor(Colors.Blue.Darken3);
                            
                            dest.Item().Text($"{package.Country}")
                                .FontSize(14)
                                .FontColor(Colors.Grey.Darken2);
                            
                            dest.Item().PaddingTop(10).Row(r =>
                            {
                                r.RelativeItem().Text($"🗓️ {package.StartDate:dd MMM yyyy} - {package.EndDate:dd MMM yyyy}")
                                    .FontSize(11);
                                r.RelativeItem().Text($"🌙 {package.Nights} nights")
                                    .FontSize(11);
                                r.RelativeItem().Text($"🎯 {package.PackageType}")
                                    .FontSize(11);
                            });
                        });

                        col.Item().PaddingVertical(15);

                        // === PASSENGER DETAILS ===
                        col.Item().Text("👤 PASSENGER DETAILS")
                            .FontSize(14)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);
                        
                        col.Item().PaddingVertical(5).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(120);
                                c.RelativeColumn();
                            });

                            AddTableRow(table, "Name:", user.FullName ?? "N/A");
                            AddTableRow(table, "Email:", user.Email ?? "N/A");
                            AddTableRow(table, "Booking Date:", booking.BookingDate.ToString("dd MMMM yyyy"));
                            AddTableRow(table, "Payment Status:", booking.Status.ToString());
                            if (booking.PaidAt.HasValue)
                                AddTableRow(table, "Paid On:", booking.PaidAt.Value.ToString("dd MMMM yyyy HH:mm"));
                        });

                        col.Item().PaddingVertical(15);

                        // === TRIP DESCRIPTION ===
                        col.Item().Text("📝 TRIP DESCRIPTION")
                            .FontSize(14)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);
                        
                        col.Item().PaddingVertical(5).Text(package.Description ?? "No description available.")
                            .FontSize(10)
                            .LineHeight(1.5f);

                        col.Item().PaddingVertical(15);

                        // === PRICING ===
                        col.Item().Text("💰 PRICING DETAILS")
                            .FontSize(14)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);
                        
                        col.Item().PaddingVertical(5).Background(Colors.Green.Lighten4).Padding(15).Column(price =>
                        {
                            if (package.IsDiscountActive && package.Discount.HasValue)
                            {
                                price.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Original Price:")
                                        .FontSize(11);
                                    r.ConstantItem(100).AlignRight().Text($"${package.Price:N2}")
                                        .FontSize(11)
                                        .Strikethrough()
                                        .FontColor(Colors.Grey.Medium);
                                });
                                
                                price.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Discount Applied:")
                                        .FontSize(11)
                                        .FontColor(Colors.Green.Darken2);
                                    r.ConstantItem(100).AlignRight().Text($"-${(package.Price - package.Discount.Value):N2}")
                                        .FontSize(11)
                                        .FontColor(Colors.Green.Darken2);
                                });

                                price.Item().PaddingTop(5).LineHorizontal(1);
                                
                                price.Item().PaddingTop(5).Row(r =>
                                {
                                    r.RelativeItem().Text("TOTAL PAID:")
                                        .FontSize(14)
                                        .Bold();
                                    r.ConstantItem(100).AlignRight().Text($"${package.EffectivePrice:N2}")
                                        .FontSize(14)
                                        .Bold()
                                        .FontColor(Colors.Green.Darken3);
                                });
                            }
                            else
                            {
                                price.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("TOTAL PAID:")
                                        .FontSize(14)
                                        .Bold();
                                    r.ConstantItem(100).AlignRight().Text($"${package.EffectivePrice:N2}")
                                        .FontSize(14)
                                        .Bold()
                                        .FontColor(Colors.Green.Darken3);
                                });
                            }
                        });

                        col.Item().PaddingVertical(15);

                        // === IMPORTANT INFORMATION ===
                        col.Item().Text("⚠️ IMPORTANT INFORMATION")
                            .FontSize(14)
                            .Bold()
                            .FontColor(Colors.Orange.Darken2);
                        
                        col.Item().PaddingVertical(5).Background(Colors.Orange.Lighten4).Padding(15).Column(info =>
                        {
                            info.Item().Text("• Please arrive at the airport 3 hours before departure").FontSize(10);
                            info.Item().Text("• Valid passport required (must be valid for at least 6 months)").FontSize(10);
                            info.Item().Text("• Check visa requirements for your destination country").FontSize(10);
                            info.Item().Text("• Travel insurance is highly recommended").FontSize(10);
                            info.Item().Text("• Keep this itinerary with you during your travels").FontSize(10);
                        });
                    });

                    // ✅ FOOTER
                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        col.Item().PaddingVertical(10).Row(row =>
                        {
                            row.RelativeItem().Text("🌍 Travel Agency - Making Dreams Come True!")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Darken1);
                            
                            row.RelativeItem().AlignRight().Text("support@travelagency.com | +1-800-TRAVEL")
                                .FontSize(9)
                                .FontColor(Colors.Grey.Medium);
                        });
                        
                        col.Item().AlignCenter().Text("Thank you for choosing us! Have a wonderful trip! ✈️🌴")
                            .FontSize(11)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);
                    });
                });
            });

            return document.GeneratePdf();
        }

        private static void AddTableRow(TableDescriptor table, string label, string value)
        {
            table.Cell().Text(label).Bold().FontSize(10);
            table.Cell().Text(value).FontSize(10);
        }
    }
}
