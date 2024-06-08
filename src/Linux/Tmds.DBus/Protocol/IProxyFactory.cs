// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

namespace Tmds.DBus.Protocol
{
    interface IProxyFactory
    {
        T CreateProxy<T>(string serviceName, ObjectPath2 path2);
    }
}