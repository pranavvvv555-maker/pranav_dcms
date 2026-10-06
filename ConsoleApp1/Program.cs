using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.EntityFrameworkCore;

QuestPDF.Settings.License = LicenseType.Community;
QuestPDF.Settings.EnableDebugging = true;

var desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
var desktopPdfPath = Path.Combine(desktopDir, "ADC_CIE_Syllabus.pdf");
var appDocPdfPath = @"d:\DCMSApp\wwwroot\docs\ADC_CIE_Syllabus.pdf";
var rootPdfPath = @"d:\DCMSApp\ADC_CIE_Syllabus.pdf";

var docsDir = @"d:\DCMSApp\wwwroot\docs";
if (!Directory.Exists(docsDir))
{
    Directory.CreateDirectory(docsDir);
}

byte[]? mitLogo = File.Exists(@"d:\DCMSApp\wwwroot\images\mit-wpu.jpg")
    ? File.ReadAllBytes(@"d:\DCMSApp\wwwroot\images\mit-wpu.jpg")
    : null;

byte[]? nirvaaLogo = File.Exists(@"d:\DCMSApp\wwwroot\images\nirvaa.jpg")
    ? File.ReadAllBytes(@"d:\DCMSApp\wwwroot\images\nirvaa.jpg")
    : null;

var document = Document.Create(container =>
{
    container.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.Margin(26, Unit.Point);
        page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor("#1e293b").FontFamily("Arial"));

        // Header
        page.Header().Column(col =>
        {
            col.Item().Row(row =>
            {
                if (mitLogo != null)
                {
                    row.ConstantItem(60).Height(32).Image(mitLogo).FitArea();
                }

                row.RelativeItem().PaddingHorizontal(8).Column(c =>
                {
                    c.Item().Text("Dr. Vishwanath Karad MIT World Peace University, Pune").FontSize(9.5f).Bold().FontColor("#0f172a");
                    c.Item().Text("Department of Computer Engineering & Technology · School of Engineering & Technology").FontSize(7.5f).FontColor("#475569");
                    c.Item().Text("Industry-Academia Collaborative M.Tech Program: MIT-WPU × NIRVAA Solutions").FontSize(7.5f).SemiBold().FontColor("#0284c7");
                });

                if (nirvaaLogo != null)
                {
                    row.ConstantItem(60).Height(32).Image(nirvaaLogo).FitArea();
                }
            });

            col.Item().PaddingTop(3).LineHorizontal(1.5f).LineColor("#0284c7");
            col.Item().Height(4);
        });

        // Content
        page.Content().Column(col =>
        {
            // Title Card
            col.Item().Border(1).BorderColor("#cbd5e1").Background("#f8fafc").Padding(7).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("M.Tech in CSE (Data Centre Systems Engineering) · AY 2026–2028 · Semester I").FontSize(8f).Bold().FontColor("#1e40af");
                    c.Item().Text("ADVANCE DATA CENTRE AND CLOUD INFRASTRUCTURE ENGINEERING (ADC & CIE)").FontSize(11f).ExtraBold().FontColor("#0f172a");
                    c.Item().Row(r =>
                    {
                        r.AutoItem().Text("Course Code: ").Bold();
                        r.AutoItem().Text("CDS40010   |   ").SemiBold().FontColor("#0284c7");
                        r.AutoItem().Text("Category: ").Bold();
                        r.AutoItem().Text("Program Major (PM)   |   ").SemiBold();
                        r.AutoItem().Text("Credits: ").Bold();
                        r.AutoItem().Text("4 (L:3, T:0, P:2)").Bold().FontColor("#16a34a");
                    });
                });

                row.ConstantItem(120).Column(c =>
                {
                    c.Item().Background("#dbeafe").Padding(3).AlignCenter().Text("ACADEMIC COUNCIL APPROVED").FontSize(6f).Bold().FontColor("#1d4ed8");
                    c.Item().PaddingTop(2).Background("#eff6ff").Padding(3).AlignCenter().Text("09 JUN 2026 · REGULAR").FontSize(6f).SemiBold().FontColor("#1e3a8a");
                });
            });

            col.Item().Height(5);

            // Teaching Scheme Table
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2.2f);
                    columns.RelativeColumn(1.1f);
                    columns.RelativeColumn(1.1f);
                    columns.RelativeColumn(1.1f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1.3f);
                    columns.RelativeColumn(3.5f);
                });

                table.Header(header =>
                {
                    header.Cell().Background("#0f172a").Padding(3).Text("Teaching Scheme").FontSize(7.5f).Bold().FontColor("#ffffff");
                    header.Cell().Background("#0f172a").Padding(3).AlignCenter().Text("Lectures").FontSize(7.5f).Bold().FontColor("#ffffff");
                    header.Cell().Background("#0f172a").Padding(3).AlignCenter().Text("Tutorial").FontSize(7.5f).Bold().FontColor("#ffffff");
                    header.Cell().Background("#0f172a").Padding(3).AlignCenter().Text("Practical").FontSize(7.5f).Bold().FontColor("#ffffff");
                    header.Cell().Background("#0f172a").Padding(3).AlignCenter().Text("Total Load").FontSize(7.5f).Bold().FontColor("#ffffff");
                    header.Cell().Background("#0f172a").Padding(3).AlignCenter().Text("Credits").FontSize(7.5f).Bold().FontColor("#ffffff");
                    header.Cell().Background("#0f172a").Padding(3).Text("Assessment Scheme (Pattern TL3)").FontSize(7.5f).Bold().FontColor("#ffffff");
                });

                table.Cell().Border(0.5f).BorderColor("#cbd5e1").Padding(3).Text("Weekly Hours").SemiBold().FontSize(7.5f);
                table.Cell().Border(0.5f).BorderColor("#cbd5e1").Padding(3).AlignCenter().Text("3 Hrs/wk").FontSize(7.5f);
                table.Cell().Border(0.5f).BorderColor("#cbd5e1").Padding(3).AlignCenter().Text("—").FontSize(7.5f);
                table.Cell().Border(0.5f).BorderColor("#cbd5e1").Padding(3).AlignCenter().Text("2 Hrs/wk").FontSize(7.5f);
                table.Cell().Border(0.5f).BorderColor("#cbd5e1").Padding(3).AlignCenter().Text("5 Hrs/wk").Bold().FontSize(7.5f);
                table.Cell().Border(0.5f).BorderColor("#cbd5e1").Padding(3).AlignCenter().Text("4 Credits").Bold().FontColor("#1d4ed8").FontSize(7.5f);
                table.Cell().Border(0.5f).BorderColor("#cbd5e1").Padding(3).Text("CCA:20 (10+10), MT:25, LCA:15 (5+5+5), TE:40").FontSize(7.5f);
            });

            col.Item().Height(3);
            col.Item().Text("Prerequisites: Computer Networks, Operating Systems, Cloud Computing Fundamentals, Intro to Data Centre Technologies, Electrical & Mechanical Fundamentals.").FontSize(6.8f).Italic().FontColor("#64748b");

            col.Item().Height(5);

            // Course Objectives & Outcomes side-by-side
            col.Item().Row(r =>
            {
                r.RelativeItem().Border(0.5f).BorderColor("#cbd5e1").Background("#f8fafc").Padding(5).Column(c =>
                {
                    c.Item().Text("COURSE OBJECTIVES (COb)").FontSize(7.8f).Bold().FontColor("#0284c7");
                    c.Item().PaddingTop(2).Text("1. Understand architecture, design standards (TIA-942), & Tier classifications.").FontSize(6.8f);
                    c.Item().Text("2. Examine cloud infrastructure models (IaaS, PaaS, SaaS, CaaS) & multi-cloud orchestration.").FontSize(6.8f);
                    c.Item().Text("3. Analyse electrical distribution, UPS topologies, redundancy, & PUE energy efficiency.").FontSize(6.8f);
                    c.Item().Text("4. Evaluate thermal cooling (CRAC/CRAH, liquid cooling, economizers, airflow management).").FontSize(6.8f);
                    c.Item().Text("5. Design high-speed network topologies, Software-Defined Networks (SDN) & spine-leaf fabrics.").FontSize(6.8f);
                    c.Item().Text("6. Apply AI/ML-driven DCIM tools for telemetry, predictive maintenance & capacity planning.").FontSize(6.8f);
                });

                r.ConstantItem(5);

                r.RelativeItem().Border(0.5f).BorderColor("#cbd5e1").Background("#f8fafc").Padding(5).Column(c =>
                {
                    c.Item().Text("COURSE OUTCOMES (CO)").FontSize(7.8f).Bold().FontColor("#16a34a");
                    c.Item().PaddingTop(2).Text("CO1: Recall & explain TIA-942/Uptime Institute tier architecture & design standards.").FontSize(6.8f);
                    c.Item().Text("CO2: Architect secure, scalable, & cost-optimised cloud infrastructures (IaaS/PaaS).").FontSize(6.8f);
                    c.Item().Text("CO3: Calculate power distribution schemes, UPS capacity, & PUE/WUE efficiency metrics.").FontSize(6.8f);
                    c.Item().Text("CO4: Design high-availability spine-leaf network topology & SDN data centre interconnects.").FontSize(6.8f);
                    c.Item().Text("CO5: Evaluate AI/ML-based DCIM platforms for telemetry, fault prediction, & reporting.").FontSize(6.8f);
                    c.Item().Text("CO6: Formulate complete end-to-end green data centre infrastructure engineering design.").FontSize(6.8f);
                });
            });

            col.Item().Height(5);

            // THEORY SYLLABUS HEADER
            col.Item().Background("#0f172a").Padding(3).PaddingLeft(6).Text("THEORY SYLLABUS (6 UNITS · 42 INSTRUCTIONAL HOURS)").FontSize(8f).Bold().FontColor("#ffffff");

            col.Item().Height(3);

            // Unit 1 & Unit 2
            col.Item().Row(r =>
            {
                r.RelativeItem().Border(0.5f).BorderColor("#e2e8f0").Padding(4).Column(c =>
                {
                    c.Item().Row(hr =>
                    {
                        hr.RelativeItem().Text("UNIT 1: Fundamentals of Data Centre Infrastructure").FontSize(7.5f).Bold().FontColor("#1e40af");
                        hr.AutoItem().Text("[6h]").FontSize(7.5f).Bold().FontColor("#64748b");
                    });
                    c.Item().PaddingTop(2).Text("• Introduction to Data Centres: Evolution, enterprise, hyperscale, colocation, & edge.").FontSize(6.8f);
                    c.Item().Text("• Facility Infrastructure: White space, grey space, structural floor loading, raised floor.").FontSize(6.8f);
                    c.Item().Text("• Standards & Tiers: TIA-942 and Uptime Institute Tier I, II, III, IV classifications.").FontSize(6.8f);
                    c.Item().Text("• Fire Protection & Physical Security: Zoning, perimeter defence, access control.").FontSize(6.8f);
                    c.Item().Text("• Data Centre Design: Site selection, seismic zoning, resilience & modular scalability.").FontSize(6.8f);
                });

                r.ConstantItem(5);

                r.RelativeItem().Border(0.5f).BorderColor("#e2e8f0").Padding(4).Column(c =>
                {
                    c.Item().Row(hr =>
                    {
                        hr.RelativeItem().Text("UNIT 2: Electrical Infrastructure Engineering").FontSize(7.5f).Bold().FontColor("#1e40af");
                        hr.AutoItem().Text("[6h]").FontSize(7.5f).Bold().FontColor("#64748b");
                    });
                    c.Item().PaddingTop(2).Text("• Utility Power Path: HV/LV substations, transformers, MLVS, bus ducts, ATS & STS switches.").FontSize(6.8f);
                    c.Item().Text("• Backup Power Systems: Diesel Generator (DG) sizing, paralleling, fuel autonomy, AMF panels.").FontSize(6.8f);
                    c.Item().Text("• UPS Systems: Online double conversion, delta conversion, VRLA vs Li-ion battery strings.").FontSize(6.8f);
                    c.Item().Text("• Redundancy Configurations: N, N+1, 2N, 2(N+1) electrical fault tolerance.").FontSize(6.8f);
                    c.Item().Text("• Power Distribution: Floor PDUs, intelligent rack PDUs, earthing, bonding, lightning protection.").FontSize(6.8f);
                });
            });

            col.Item().Height(3);

            // Unit 3 & Unit 4
            col.Item().Row(r =>
            {
                r.RelativeItem().Border(0.5f).BorderColor("#e2e8f0").Padding(4).Column(c =>
                {
                    c.Item().Row(hr =>
                    {
                        hr.RelativeItem().Text("UNIT 3: Mechanical Infrastructure, HVAC & Thermal Mgmt").FontSize(7.5f).Bold().FontColor("#1e40af");
                        hr.AutoItem().Text("[8h]").FontSize(7.5f).Bold().FontColor("#64748b");
                    });
                    c.Item().PaddingTop(2).Text("• HVAC Fundamentals: Psychrometric chart, ASHRAE TC 9.9 thermal envelopes (A1–A4).").FontSize(6.8f);
                    c.Item().Text("• Precision Cooling: CRAC (DX downflow/upflow) vs CRAH (chilled water plant loops).").FontSize(6.8f);
                    c.Item().Text("• Airflow Optimization: Hot Aisle / Cold Aisle containment (HACC/CACC), perforated tiles.").FontSize(6.8f);
                    c.Item().Text("• Liquid Cooling: In-row units, rear-door heat exchangers (RDHx), direct-to-chip, immersion.").FontSize(6.8f);
                    c.Item().Text("• Energy Efficient Cooling: Economizers (air/water free cooling), PUE calculation.").FontSize(6.8f);
                });

                r.ConstantItem(5);

                r.RelativeItem().Border(0.5f).BorderColor("#e2e8f0").Padding(4).Column(c =>
                {
                    c.Item().Row(hr =>
                    {
                        hr.RelativeItem().Text("UNIT 4: Networking, Security & Operations").FontSize(7.5f).Bold().FontColor("#1e40af");
                        hr.AutoItem().Text("[8h]").FontSize(7.5f).Bold().FontColor("#64748b");
                    });
                    c.Item().PaddingTop(2).Text("• Networking Fabric: Spine-Leaf topology, Top-of-Rack (ToR), End-of-Row (EoR), SDN.").FontSize(6.8f);
                    c.Item().Text("• Structured Cabling: TIA-606-C labelling, OM4/OS2 fiber optics, Cat6A copper, patch racks.").FontSize(6.8f);
                    c.Item().Text("• Physical Security: Multi-layer defence, biometric readers, man-traps, CCTV coverage.").FontSize(6.8f);
                    c.Item().Text("• Fire & Life Safety: VESDA aspirating smoke detection, clean agent suppression (FM-200/Novec).").FontSize(6.8f);
                    c.Item().Text("• Operations & DR: Incident response, standard SOPs, disaster recovery, business continuity.").FontSize(6.8f);
                });
            });

            col.Item().Height(3);

            // Unit 5 & Unit 6
            col.Item().Row(r =>
            {
                r.RelativeItem().Border(0.5f).BorderColor("#e2e8f0").Padding(4).Column(c =>
                {
                    c.Item().Row(hr =>
                    {
                        hr.RelativeItem().Text("UNIT 5: Intelligent DCIM, Automation & Sustainability").FontSize(7.5f).Bold().FontColor("#1e40af");
                        hr.AutoItem().Text("[8h]").FontSize(7.5f).Bold().FontColor("#64748b");
                    });
                    c.Item().PaddingTop(2).Text("• DCIM Platforms: Real-time telemetry, asset tracking, power chain mapping, thermal mapping.").FontSize(6.8f);
                    c.Item().Text("• Building Management Systems: BACnet, Modbus, SNMP environmental sensor integration.").FontSize(6.8f);
                    c.Item().Text("• AI/ML Operations: Predictive maintenance, anomaly detection, digital twin facility models.").FontSize(6.8f);
                    c.Item().Text("• Green Data Centres: Renewable energy (solar PPA), PUE/WUE/CUE metrics, carbon accounting.").FontSize(6.8f);
                    c.Item().Text("• Compliance & Governance: ISO 27001, SOC 2, LEED certification, statutory regulatory audit.").FontSize(6.8f);
                });

                r.ConstantItem(5);

                r.RelativeItem().Border(0.5f).BorderColor("#e2e8f0").Padding(4).Column(c =>
                {
                    c.Item().Row(hr =>
                    {
                        hr.RelativeItem().Text("UNIT 6: Advanced Cloud Infrastructure & Hybrid Cloud").FontSize(7.5f).Bold().FontColor("#1e40af");
                        hr.AutoItem().Text("[8h]").FontSize(7.5f).Bold().FontColor("#64748b");
                    });
                    c.Item().PaddingTop(2).Text("• Cloud Service Models: IaaS, PaaS, SaaS, Containers as a Service (CaaS), Serverless (FaaS).").FontSize(6.8f);
                    c.Item().Text("• Deployment Architectures: Public, Private, Hybrid Cloud, Multi-Cloud workload orchestration.").FontSize(6.8f);
                    c.Item().Text("• Software-Defined DC: Compute virtualization (KVM/ESXi), Storage spaces, SDN overlays (VXLAN).").FontSize(6.8f);
                    c.Item().Text("• Connectivity & Security: IPsec VPN tunnels, AWS Direct Connect, Azure ExpressRoute, Zero Trust.").FontSize(6.8f);
                    c.Item().Text("• Infrastructure as Code: Terraform declarative provisioning, cloud monitoring & telemetry.").FontSize(6.8f);
                });
            });

            // Clean page break before Practicals
            col.Item().PageBreak();

            // PRACTICALS SECTION
            col.Item().Background("#15803d").Padding(3.5f).PaddingLeft(6).Text("LIST OF LABORATORY EXPERIMENTS & PRACTICALS (10 EXPERIMENTS · 2 HRS/WEEK)").FontSize(8.5f).Bold().FontColor("#ffffff");

            col.Item().Height(4);

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(28);
                    columns.RelativeColumn(3.2f);
                    columns.RelativeColumn(6.8f);
                });

                table.Header(header =>
                {
                    header.Cell().Background("#f1f5f9").Padding(3).AlignCenter().Text("Exp").Bold().FontSize(7.5f);
                    header.Cell().Background("#f1f5f9").Padding(3).Text("Practical Title").Bold().FontSize(7.5f);
                    header.Cell().Background("#f1f5f9").Padding(3).Text("Practical Scope & Deliverables").Bold().FontSize(7.5f);
                });

                var practicals = new[]
                {
                    ("1", "Physical Layout & Functional Zoning", "Study physical layout, security perimeters, and functional zones (white space, UPS room, cooling yard, NOC, loading bay) of a Tier III/Tier IV data centre."),
                    ("2", "UPS, Battery & DG Sizing Calculations", "Perform IT load estimation, calculate UPS kVA capacity (with 20% headroom), battery bank AH capacity (VRLA/Li-ion), and Diesel Generator kVA sizing."),
                    ("3", "Electrical Single Line Diagram (SLD)", "Prepare and trace an electrical Single Line Diagram (SLD) from utility substation, transformers, ATS/STS switches, UPS buses to floor PDUs and server racks."),
                    ("4", "Airflow Management & Aisle Containment", "Design hot aisle–cold aisle containment layout (HACC/CACC), analyze perforated tile placement, and compute airflow (CFM) requirements for 5–10 kW/rack."),
                    ("5", "CRAC, CRAH & Precision Cooling Systems", "Study CRAC (direct expansion DX) and CRAH (chilled water) precision cooling units, primary/secondary cooling loops, and calculate sensible heat removal."),
                    ("6", "Structured Cabling & Rack Layout", "Design structured cabling architecture (TIA-606-C), overhead cable tray routing, OM4 multimode fibre and Cat6A copper patch panel termination schemes."),
                    ("7", "Virtual Machine Deployment on Cloud", "Provision and configure Virtual Machines on cloud platforms (AWS / Azure), establish virtual network (VNet/VPC), Network Security Groups (NSGs), and SSH/RDP access."),
                    ("8", "Infrastructure as Code (IaC) via Terraform", "Author Terraform configuration files (.tf) to automate multi-tier cloud infrastructure provisioning (Compute, Storage, Security Groups, and Subnets)."),
                    ("9", "DCIM Software Setup & Telemetry Monitoring", "Install and configure a DCIM tool (e.g. OpenDCIM), import rack asset inventory, map power/cooling topology, and monitor real-time PUE and temperature sensors."),
                    ("10", "Hybrid Cloud & Disaster Recovery Architecture", "Design a hybrid cloud architecture connecting on-premise data centre resources to cloud VPC via site-to-site IPsec VPN, configure automated backup and disaster recovery.")
                };

                foreach (var (no, title, desc) in practicals)
                {
                    table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2.5f).AlignCenter().Text(no).Bold().FontColor("#0369a1").FontSize(7.2f);
                    table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2.5f).Text(title).Bold().FontColor("#0f172a").FontSize(7.2f);
                    table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2.5f).Text(desc).FontSize(7f).FontColor("#334155");
                }
            });

            col.Item().Height(6);

            // References & Textbooks
            col.Item().Border(0.5f).BorderColor("#cbd5e1").Background("#f8fafc").Padding(5).Row(r =>
            {
                r.RelativeItem().Column(c =>
                {
                    c.Item().Text("RECOMMENDED TEXTBOOKS & REFERENCE BOOKS").FontSize(7.8f).Bold().FontColor("#0f172a");
                    c.Item().PaddingTop(2).Text("1. Hwaiyu Geng, 'Data Center Handbook', 2nd Edition, Wiley, 2021.").FontSize(7f);
                    c.Item().Text("2. Mauricio Arregoces & Maurizio Portolani, 'Data Center Fundamentals', Cisco Press, 2003.").FontSize(7f);
                    c.Item().Text("3. Thomas Erl et al., 'Cloud Computing: Concepts, Technology & Architecture', Pearson, 2013.").FontSize(7f);
                    c.Item().Text("4. Michael J. Kavis, 'Architecting the Cloud: Service Models & Decisions', Wiley, 2018.").FontSize(7f);
                    c.Item().Text("5. Yevgeniy Brikman, 'Terraform: Up & Running', 3rd Edition, O'Reilly Media, 2022.").FontSize(7f);
                });

                r.ConstantItem(6);

                r.RelativeItem().Column(c =>
                {
                    c.Item().Text("ONLINE & INDUSTRY BENCHMARK RESOURCES").FontSize(7.8f).Bold().FontColor("#0f172a");
                    c.Item().PaddingTop(2).Text("• The Green Grid: thegreengrid.org (PUE & DC Energy Metrics)").FontSize(7f);
                    c.Item().Text("• Cisco Data Center: cisco.com (ACI, Spine-Leaf Architecture)").FontSize(7f);
                    c.Item().Text("• Microsoft Learn: learn.microsoft.com (Azure Cloud Infrastructure)").FontSize(7f);
                    c.Item().Text("• VMware SDDC: docs.vmware.com (Virtualization & SDDC)").FontSize(7f);
                    c.Item().Text("• Cloud Native Computing Foundation: cncf.io (Kubernetes & CaaS)").FontSize(7f);
                });
            });

            col.Item().Height(6);

            // Examination Evaluation Pattern Table
            col.Item().Border(0.5f).BorderColor("#cbd5e1").Background("#ffffff").Padding(5).Column(c =>
            {
                c.Item().Text("EVALUATION & ASSESSMENT SCHEME (PATTERN TL3)").FontSize(7.8f).Bold().FontColor("#0f172a");
                c.Item().PaddingTop(2).Row(er =>
                {
                    er.RelativeItem().Text("• Class Continuous Assessment 1 (CCA1): 10 Marks").FontSize(7.2f);
                    er.RelativeItem().Text("• Mid Term Exam (MT): 25 Marks").FontSize(7.2f);
                    er.RelativeItem().Text("• Class Continuous Assessment 2 (CCA2): 10 Marks").FontSize(7.2f);
                });
                c.Item().Row(er =>
                {
                    er.RelativeItem().Text("• Lab Continuous Assessment 1 (LCA1): 5 Marks").FontSize(7.2f);
                    er.RelativeItem().Text("• Lab Continuous Assessment 2 (LCA2): 5 Marks").FontSize(7.2f);
                    er.RelativeItem().Text("• Lab Continuous Assessment 3 (LCA3): 5 Marks").FontSize(7.2f);
                });
                c.Item().Row(er =>
                {
                    er.RelativeItem().Text("• Term End Examination (TE): 40 Marks (3 Hours)").FontSize(7.2f).Bold().FontColor("#1e40af");
                    er.RelativeItem(2).Text("• Total Course Weightage: 100 Marks (Passing Minimum: 50% Aggregate)").FontSize(7.2f).Bold().FontColor("#16a34a");
                });
            });

            col.Item().Height(6);

            // Signatories Row
            col.Item().Border(0.5f).BorderColor("#cbd5e1").Background("#f8fafc").Padding(6).Row(r =>
            {
                r.RelativeItem().Column(c =>
                {
                    c.Item().Text("Dr. Vivekanand M. Bankolli / Mr. Siddu Patil").FontSize(7.5f).Bold().FontColor("#0f172a");
                    c.Item().Text("Subject Experts & Faculty (ADC & CIE)").FontSize(6.8f).FontColor("#475569");
                    c.Item().Text("NIRVAA Solutions Pvt. Ltd.").FontSize(6.5f).SemiBold().FontColor("#0284c7");
                });

                r.RelativeItem().AlignCenter().Column(c =>
                {
                    c.Item().Text("Prof. Dr. B. M. Patil").FontSize(7.5f).Bold().FontColor("#991b1b");
                    c.Item().Text("Program Director, DoME").FontSize(6.8f).FontColor("#475569");
                    c.Item().Text("MIT-WPU, Pune").FontSize(6.5f).SemiBold().FontColor("#0f172a");
                });

                r.RelativeItem().AlignRight().Column(c =>
                {
                    c.Item().Text("Prof. Dr. Siddharth S. Chakrabarti").FontSize(7.5f).Bold().FontColor("#991b1b");
                    c.Item().Text("Dean, School of Engineering & Technology (SoET)").FontSize(6.8f).FontColor("#475569");
                    c.Item().Text("MIT-WPU, Pune").FontSize(6.5f).SemiBold().FontColor("#0f172a");
                });
            });
        });

        // Footer
        page.Footer().Row(row =>
        {
            row.RelativeItem().Text("M.Tech CSE (Data Centre Systems Engineering) · Course: CDS40010 · MIT-WPU × NIRVAA Solutions").FontSize(7f).FontColor("#94a3b8");
            row.AutoItem().Text(x =>
            {
                x.Span("Page ");
                x.CurrentPageNumber();
                x.Span(" of ");
                x.TotalPages();
            });
        });
    });
});

document.GeneratePdf(desktopPdfPath);
Console.WriteLine($"Generated at: {desktopPdfPath}");

document.GeneratePdf(appDocPdfPath);
Console.WriteLine($"Generated at: {appDocPdfPath}");

document.GeneratePdf(rootPdfPath);
Console.WriteLine($"Generated at: {rootPdfPath}");

var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<DCMSApp.Data.AppDbContext>()
    .UseSqlite(@"Data Source=d:\DCMSApp\dcms.db")
    .Options;
using var db = new DCMSApp.Data.AppDbContext(options);
try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE Sessions ADD COLUMN IsOnline INTEGER NOT NULL DEFAULT 0;"); } catch { }
try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE Sessions ADD COLUMN MeetingPlatform TEXT NULL;"); } catch { }
try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE Sessions ADD COLUMN MeetingLink TEXT NULL;"); } catch { }
await DCMSApp.Data.SeedData.InitializeAsync(db);

var sessionsCount = db.Sessions.Count(s => s.Date >= new DateTime(2026, 9, 11) && s.Date <= new DateTime(2026, 10, 4));
var totalHours = db.Sessions.Where(s => s.Date >= new DateTime(2026, 9, 11) && s.Date <= new DateTime(2026, 10, 4)).Sum(s => s.DurationHours);
var lineItemsSum = db.PaymentLineItems.Where(l => l.PaymentPeriod.PeriodName == "11 September - 04 October 2026").Sum(l => l.NetPayable);

Console.WriteLine($"Reseed successful! Sessions: {sessionsCount}, Total Hours: {totalHours}, Total Payment: Rs. {lineItemsSum}");
