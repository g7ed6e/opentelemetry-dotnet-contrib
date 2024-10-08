// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

// <auto-generated>This file has been auto generated from 'src\OpenTelemetry.SemanticConventions\scripts\templates\registry\SemanticConventionsAttributes.cs.j2' </auto-generated>

#nullable enable

#pragma warning disable CS1570 // XML comment has badly formed XML

namespace OpenTelemetry.SemanticConventions;

/// <summary>
/// Constants for semantic attribute names outlined by the OpenTelemetry specifications.
/// </summary>
public static class OtherAttributes
{
    /// <summary>
    /// Deprecated, use <c>db.client.connections.state</c> instead
    /// </summary>
    [Obsolete("Replaced by <c>db.client.connections.state</c>")]
    public const string AttributeState = "state";

    /// <summary>
    /// Deprecated, use <c>db.client.connections.state</c> instead
    /// </summary>
    public static class StateValues
    {
        /// <summary>
        /// idle
        /// </summary>
        [Obsolete("Replaced by <c>db.client.connections.state</c>")]
        public const string Idle = "idle";

        /// <summary>
        /// used
        /// </summary>
        [Obsolete("Replaced by <c>db.client.connections.state</c>")]
        public const string Used = "used";
    }
}
