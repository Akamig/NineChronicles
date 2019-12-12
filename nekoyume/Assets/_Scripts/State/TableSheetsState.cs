using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;

namespace Nekoyume.State
{
    public class TableSheetsState : State, IEquatable<TableSheetsState>
    {
        public static readonly Address Address = new Address(new byte[]
            {
                0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x3
            }
        );

        private IValue _serialized;

        private int _hashCode;

        // key = TableSheet Name / value = TableSheet csv.
        public IImmutableDictionary<string, string> TableSheets { get; }

        public TableSheetsState() : base(Address)
        {
            TableSheets = new Dictionary<string, string>().ToImmutableDictionary();
        }

        public TableSheetsState(IDictionary<string, string> sheets) : base(Address)
        {
            TableSheets = sheets.ToImmutableDictionary();
            _serialized = Serialize();

            int ComputeHash(byte[] bytes)
            {
                unchecked
                {
                    var result = 0;
                    foreach (byte b in bytes)
                    {
                        result = (result*31) ^ b;
                    }
                    return result;
                }
            }
            
            _hashCode = _serialized
                .EncodeIntoChunks()
                .Aggregate(0, (prev, bytes) => prev ^ ComputeHash(bytes));
        }

        public TableSheetsState(Bencodex.Types.Dictionary serialized) 
            : this(
                serialized
                .GetValue<Bencodex.Types.Dictionary>("table_sheets")
                .ToDictionary(pair => (string) (Text) pair.Key, pair => (string) (Text) pair.Value))
        {
        }

        public TableSheetsState UpdateTableSheet(string name, string csv)
        {
            var updatedSheets = TableSheets.SetItem(name, csv);
            return new TableSheetsState(updatedSheets.ToDictionary(kv => kv.Key, kv => kv.Value));
        }

        public override IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "table_sheets"] = new Bencodex.Types.Dictionary(TableSheets.Select(pair =>
                    new KeyValuePair<IKey, IValue>((Text) pair.Key, (Text) pair.Value)))
            }.Union((Bencodex.Types.Dictionary) base.Serialize()));

        public static TableSheetsState Current
        {
            get
            {
                var d = Game.Game.instance.agent.GetState(Address);
                if (d == null)
                {
                    return new TableSheetsState();
                }
                else
                {
                    return new TableSheetsState((Bencodex.Types.Dictionary)d);
                }
            }
        }

        public static TableSheetsState FromActionContext(IActionContext ctx)
        {
            var serialized = ctx.PreviousStates.GetState(Address);
            if (serialized == null)
            {
                return new TableSheetsState();
            }
            else
            {
                return new TableSheetsState((Bencodex.Types.Dictionary)serialized);
            }
        }

        public override bool Equals(object other)
        {
            if (other is TableSheetsState otherState) 
            {
                return Equals(otherState);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public bool Equals(TableSheetsState other)
        {
            return _serialized?.Equals(other._serialized) ?? false;
        }
    }
}
