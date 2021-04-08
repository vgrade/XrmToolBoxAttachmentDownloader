using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using XrmToolBox.Extensibility;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk.Metadata;

namespace XrmToolBox.AttachmentsDownloader
{
    public class AttachmentHelper
    {

        public static string _PrimaryIdAttribute;

        public static string _PrimaryNameAttribute;

        public static QueryExpression GetQueryExpression(IOrganizationService Service, string _fetchXml)
        {
            var conversionRequest = new FetchXmlToQueryExpressionRequest
            {
                FetchXml = _fetchXml
            };
            var conversionResponse =
                (FetchXmlToQueryExpressionResponse)Service.Execute(conversionRequest);
            QueryExpression queryExpression = conversionResponse.Query;

            QueryExpression qe = new QueryExpression("annotation");

            LinkEntity _linkEntity = new LinkEntity();
            _linkEntity.JoinOperator = JoinOperator.Inner;
            _linkEntity.LinkCriteria = queryExpression.Criteria;
            foreach (var a in queryExpression.LinkEntities)
            {
                _linkEntity.LinkEntities.Add(a);
            }
            _linkEntity.LinkFromAttributeName = "objectid";
            GetPrimaryIdAttribute(Service, queryExpression.EntityName, out _PrimaryIdAttribute, out _PrimaryNameAttribute);
            _linkEntity.LinkToAttributeName = _PrimaryIdAttribute;

            _linkEntity.LinkToEntityName = queryExpression.EntityName;
            qe.LinkEntities.Add(_linkEntity);

            List<string> _columns = new List<string>();
            foreach (var a in queryExpression.ColumnSet.Columns)
            {
                _columns.Add(a);
            }
            if (!_columns.Contains(_PrimaryNameAttribute))
            {
                _columns.Add(_PrimaryNameAttribute);
            }
            qe.LinkEntities[0].Columns.AddColumns(_columns.ToArray());
            qe.LinkEntities[0].EntityAlias = "Regarding";
            qe.ColumnSet = new ColumnSet(true);
            return qe;

        }

        public static int GetToalRecordsCount(IOrganizationService Service, string _fetchXml)
        {
            int result = 0;
            var conversionRequest = new FetchXmlToQueryExpressionRequest
            {
                FetchXml = _fetchXml
            };
            var conversionResponse =
                (FetchXmlToQueryExpressionResponse)Service.Execute(conversionRequest);
            QueryExpression queryExpression = conversionResponse.Query;

            QueryExpression qe = new QueryExpression("annotation");

            LinkEntity _linkEntity = new LinkEntity();
            _linkEntity.JoinOperator = JoinOperator.Inner;
            _linkEntity.LinkCriteria = queryExpression.Criteria;
            foreach (var a in queryExpression.LinkEntities)
            {
                _linkEntity.LinkEntities.Add(a);
            }
            _linkEntity.LinkFromAttributeName = "objectid";
            GetPrimaryIdAttribute(Service, queryExpression.EntityName, out _PrimaryIdAttribute, out _PrimaryNameAttribute);
            _linkEntity.LinkToAttributeName = _PrimaryIdAttribute;

            _linkEntity.LinkToEntityName = queryExpression.EntityName;
            qe.LinkEntities.Add(_linkEntity);

            List<string> _columns = new List<string>();
            foreach (var a in queryExpression.ColumnSet.Columns)
            {
                _columns.Add(a);
            }
            if (!_columns.Contains(_PrimaryNameAttribute))
            {
                _columns.Add(_PrimaryNameAttribute);
            }
            qe.LinkEntities[0].Columns.AddColumns(_columns.ToArray());
            qe.LinkEntities[0].EntityAlias = "Regarding";
            qe.ColumnSet = new ColumnSet(false);

            int queryCount = 5000;
            int pageNumber = 1;

            qe.PageInfo = new PagingInfo();
            qe.PageInfo.Count = queryCount;
            qe.PageInfo.PageNumber = pageNumber;
            qe.PageInfo.PagingCookie = null;
            qe.PageInfo.ReturnTotalRecordCount = true;

            while (true)
            {
                EntityCollection enColl = Service.RetrieveMultiple(qe);

                result += enColl.Entities.Count;
                if (enColl.MoreRecords)
                {
                    qe.PageInfo.PageNumber = ++pageNumber;
                    qe.PageInfo.PagingCookie = enColl.PagingCookie;
                    qe.PageInfo.ReturnTotalRecordCount = true;
                }
                else
                {
                    break;
                }
            }
            return result;

        }

        public static void GetPrimaryIdAttribute(IOrganizationService service, String LogicalName, out string PrimaryIdAttribute, out string PrimaryNameAttribute)
        {
            RetrieveEntityRequest retrieveEntityRequest = new RetrieveEntityRequest
            {
                EntityFilters = EntityFilters.All,
                LogicalName = LogicalName
            };
            RetrieveEntityResponse retrieveAccountEntityResponse = (RetrieveEntityResponse)service.Execute(retrieveEntityRequest);
            EntityMetadata AccountEntity = retrieveAccountEntityResponse.EntityMetadata;
            PrimaryIdAttribute = AccountEntity.PrimaryIdAttribute;
            PrimaryNameAttribute = AccountEntity.PrimaryNameAttribute;
        }
    }
}
