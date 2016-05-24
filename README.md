Sitecore LinkList Field Type.
========

## Updates

This project has been updated to build with Sitecore 7 and .NET 4.5
Guaranteed to work for Glass.Mapper.Sc 3.0.5.14

#### Additions

- Link now has a LinkFieldType enum property for External/Media/Internal

#### Glass Mapper Integration

In GlassMapperScCustom.cs add the following to the CastleConfig method.

    container.Register(
      Component.For<AbstractDataMapper>().ImplementedBy<LinkListDataHandler>().LifeStyle.Transient
    );
    
#### Multiple assemblies

If you have your models in a seperate assembly than your web project, take a look at this link for further details:
http://glass.lu/docs/tutorial/sitecore/tutorial20/tutorial20.html

#### Sitecore 8.1

For a compatible version of this package with 8.1 download the zip: Horsch_Monoco_CMS_Glass_Mapper-8.1.zip