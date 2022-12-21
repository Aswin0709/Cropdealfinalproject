using CaseStudy.Dtos;
using CaseStudy.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using System.Net.Mail;
using System.Net;
using System.Web.Http.ModelBinding;
using System.Security.Cryptography;

namespace CaseStudy.Repository
{
    public class InvoiceRepository
    {
        DatabaseContext _context;
        ExceptionRepository _exception;
        public InvoiceRepository(DatabaseContext cotnext, ExceptionRepository exception)
        {
            _context = cotnext;
            _exception = exception;
        }
        #region CreateInvoice
        /// <summary>
        /// Creates a new Invoice
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<ActionResult<Invoice>> CreateInvoice(InvoiceDto data)
        {
            try
            {
                var farm = _context.Users.SingleOrDefault(a => a.UserId == data.FarmerId);
                var deal = _context.Users.SingleOrDefault(a => a.UserId == data.DealerId);
                var dealacc = _context.Users.Include("Account")
                    .SingleOrDefault(a => a.UserId == data.DealerId).Account.AccountNumber;

                var farmacc = _context.Users.Include("Account")
                    .SingleOrDefault(a => a.UserId == data.FarmerId).Account.AccountNumber;

                var crop = _context.CropDetails.Include("CropType")
                    .SingleOrDefault(a => a.CropId == data.CropId);

                var invoice = new Invoice();
                invoice.Amount = crop.ExpectedPrice;
                invoice.DealerId = data.DealerId;
                invoice.FarmerId = data.FarmerId;
                invoice.CropId = data.CropId;
                invoice.InvoiceDate = DateTime.Now;

                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();



                if (farm == null || deal == null)
                {
                    return null;
                }

                //Sending to Farmer Receipt
                SendMailFarmer(invoice, farmacc,dealacc, crop);
                //Sending to Dealer Invoice
                SendMailDealer(invoice, dealacc,farmacc, crop);

                return invoice;
            }
            catch(Exception e)
            {
                await _exception.AddException(e, "CreateInvoice in InvoiceRepo");
                return null;
            }
        }
        #endregion

        #region FarmerInvoices
        /// <summary>
        /// Invoices Based on Id for farmer
        /// </summary>
        /// <param name="fid"></param>
        /// <returns></returns>
        public async Task<IEnumerable<FarmerReceipt>> FarmerInvoices(int fid)
        {
            try {
                var invoices = await _context.Invoices.Where(a => a.FarmerId == fid)
                    .OrderBy(p => p.InvoiceDate)
                    .ToListAsync();
                var receipts = new List<FarmerReceipt>();
                foreach (Invoice p in invoices)
                {
                    var crop = _context.CropDetails.Include("CropType").SingleOrDefault(a => a.CropId == p.CropId);
                    var farmer = _context.Accounts.SingleOrDefault(a => a.UserId == fid);
                    var deal = _context.Accounts.SingleOrDefault(a => a.UserId == p.DealerId);

                    var receipt = new FarmerReceipt()
                    {
                        InvoiceDate = p.InvoiceDate,
                        InvoiceId = p.InvoiceId,
                        CropName = crop.CropName,
                        CropType = crop.CropType.TypeName,
                        DealerAccNumber = deal.AccountNumber,
                        FarmerAccNumber = farmer.AccountNumber,
                        Price = crop.ExpectedPrice,
                        Quantity = crop.QtyAvailable
                    };
                    receipts.Add(receipt);
                }

                if (invoices.Count < 0)
                {
                    return null;
                }
                return receipts;
            }
            catch (Exception e)
            {
                await _exception.AddException(e, "FarmerInvoices in InvoiceRepo");
                return null;
            }
        }
        #endregion

        #region
        /// <summary>
        /// Invoices Based on Id for Dealer
        /// </summary>
        /// <param name="did"></param>
        /// <returns></returns>
        public async Task<IEnumerable<FarmerReceipt>> DealerInvoices(int did)
        {
            try
            {
                var invoices = await _context.Invoices.Where(a => a.DealerId == did)
                    .Select(p => new FarmerReceipt()
                    {
                        InvoiceDate = p.InvoiceDate,
                        InvoiceId = p.InvoiceId,
                        CropName = _context.CropDetails.SingleOrDefault(a => a.CropId == p.CropId).CropName,
                        CropType = _context.CropDetails.Include("CropType").SingleOrDefault(a => a.CropId == p.CropId).CropType.TypeName,
                        FarmerAccNumber = _context.Accounts.SingleOrDefault(a => a.UserId == p.FarmerId).AccountNumber,
                        DealerAccNumber = _context.Accounts.SingleOrDefault(a => a.UserId == did).AccountNumber,
                        Quantity = _context.CropDetails.SingleOrDefault(a => a.CropId == p.CropId).QtyAvailable,
                        Price = _context.CropDetails.SingleOrDefault(a => a.CropId == p.CropId).ExpectedPrice
                    })
                    .ToListAsync();

                if (invoices.Count < 0)
                {
                    return null;
                }
                return invoices;
            }
            catch(Exception e)
            {
                await _exception.AddException(e, "DealerInvoices in InvoiceRepo");
                return null;
            }
        }
        #endregion
        #region
        /// <summary>
        /// Send Email to Farmer
        /// </summary>
        /// <param name="invoice"></param>
        /// <param name="To"></param>
        /// <param name="crop"></param>
        public async void SendMailFarmer(Invoice invoice, long farmacc, long dealacc, CropDetail crop)
        {
            try
            {
                using (MailMessage message = new MailMessage("aswinvalorant@gmail.com", "farmeranddealer@gmail.com"))
                {
                    
                    message.Body = "Successfull Transaction" +
                        $"Crop Name: {crop.CropName}\n" +
                        $"Crop Type: {crop.CropType.TypeName}\n" +
                        $"Crop Qty: {crop.QtyAvailable}\n" +
                        $"Your Account Number: {farmacc}\n" +
                        $"Dealer Account Number: {dealacc}\n" +
                        $"Amount: {invoice.Amount}\n" +
                        $"Invoice Id: {invoice.InvoiceId}\n"
                        ;

                    message.Subject = "Here Is Your Receipt";
                    message.IsBodyHtml = false;

                    using (SmtpClient smtp = new SmtpClient())
                    {
                        smtp.Host = "smtp.gmail.com";
                        smtp.EnableSsl = true;
                        NetworkCredential cred = new NetworkCredential("aswinvalorant@gmail.com", "zzbneltxfucavjlm");
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = cred;
                        smtp.Port = 587;
                        smtp.Send(message);
                    }
                }
            }
            catch (Exception e)
            {
              await  _exception.AddException(e, "Send Email to Farmer in InvoiceRepo");
            }
        }
        #endregion

        #region
        /// <summary>
        /// SEnd email to Dealer
        /// </summary>
        /// <param name="invoice"></param>
        /// <param name="To"></param>
        /// <param name="crop"></param>
        public async void SendMailDealer(Invoice invoice, long dealacc, long farmacc,  CropDetail crop)
        {
            try
            {
                using (MailMessage message = new MailMessage("aswinvalorant@gmail.com", "farmeranddealer@gmail.com"))
                {
                    message.Body = "Successfull Transaction" +
                        $"Crop Name: {crop.CropName}\n" +
                        $"Crop Type: {crop.CropType.TypeName}\n" +
                        $"Crop Qty: {crop.QtyAvailable}\n" +
                        $"Farmer Account Number: {farmacc}\n" +
                        $"Your Account Number: {dealacc}\n" +
                        $"Amount: {invoice.Amount}\n" +
                        $"Invoice Id: {invoice.InvoiceId}\n"
                        ;

                    message.Subject = "Here Is Your Receipt";
                    message.IsBodyHtml = false;

                    using (SmtpClient smtp = new SmtpClient())
                    {
                        smtp.Host = "smtp.gmail.com";
                        smtp.EnableSsl = true;
                        NetworkCredential cred = new NetworkCredential("aswinvalorant@gmail.com", "zzbneltxfucavjlm");
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = cred;
                        smtp.Port = 587;
                        smtp.Send(message);
                    }
                }
            }
            catch (Exception e)
            {
               await _exception.AddException(e, "Send Email to Dealer in InvoiceRepo");
            }
        }
        #endregion
    }
}
